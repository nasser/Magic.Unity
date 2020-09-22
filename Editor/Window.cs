using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

using clojure.lang;

using UnityEngine;
using UnityEditor;

namespace Magic.Unity
{
    public class Window : EditorWindow
    {
        const string DefaultOutFolder = "Assets/MagicBuild";
        static string[] DefaultLinkXmlEntries = new[] {
            "Magic.Runtime",
            "Clojure",
            "clojure.clr.io.clj",
            "clojure.core.clj",
            "clojure.core_clr.clj",
            "clojure.core_deftype.clj",
            "clojure.core_print.clj",
            "clojure.core.protocols.clj",
            "clojure.core_proxy.clj",
            "clojure.edn.clj",
            "clojure.genclass.clj",
            "clojure.gvec.clj",
            "clojure.instant.clj",
            "clojure.set.clj",
            "clojure.spec.alpha.clj",
            "clojure.spec.gen.alpha.clj",
            "clojure.stacktrace.clj",
            "clojure.string.clj",
            "clojure.template.clj",
            "clojure.test.clj",
            "clojure.uuid.clj",
            "clojure.walk.clj"
        };

        const string EditorPerfsKey = "Magic.Unity.CompilerWindow.Vaues";
        [SerializeField] List<string> paths = new List<string>();
        [SerializeField] List<string> namespaces = new List<string>();
        [SerializeField] string outFolder = DefaultOutFolder;
        [SerializeField] bool autogenerateLinkXml = true;
        [SerializeField] List<string> linkXmlEntries = new List<string>(DefaultLinkXmlEntries);
        [SerializeField] bool showAdvanced = false;
        [SerializeField] bool verbose = false;

        [MenuItem("MAGIC/Compiler...")]
        static void Init()
        {
            EditorWindow.GetWindow<Window>().Show();
        }

        void RenderStringListView(List<string> list)
        {
            var indexToClear = -1;
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = EditorGUILayout.TextField(list[i]);
                if (GUILayout.Button(new GUIContent("-", "Remove this entry"), GUILayout.Width(20)))
                {
                    indexToClear = i;
                }

                EditorGUILayout.EndHorizontal();
            }

            if (indexToClear >= 0)
            {
                list.RemoveAt(indexToClear);
                SaveState();
            }

            if (GUILayout.Button(new GUIContent("+", "Add an entry")))
            {
                list.Add("");
                SaveState();
            }
        }

        void SaveState()
        {
            EditorPrefs.SetString(EditorPerfsKey, EditorJsonUtility.ToJson(this));
        }

        void OnDisable()
        {
            SaveState();
        }

        static Var MagicCompilerNamespaceVar;
        static string MagicIL2CPPCLIExePath = null;

        void OnEnable()
        {
            RuntimeBootstrapFlag.SkipSpecChecks = true;
            RuntimeBootstrapFlag._startDefaultServer = false;
            RT.var("clojure.core", "require").invoke(Symbol.intern("magic.api"));
            // HACK
            RT.var("clojure.core", "require").invoke(Symbol.intern("clojure.edn"));
            RT.var("clojure.core", "require").invoke(Symbol.intern("clojure.walk"));
            RT.var("clojure.core", "require").invoke(Symbol.intern("clojure.data"));
            MagicCompilerNamespaceVar = RT.var("magic.api", "compile-namespace");
            EditorJsonUtility.FromJsonOverwrite(EditorPrefs.GetString(EditorPerfsKey), this);
            MagicIL2CPPCLIExePath = FindMagicIL2CPPCLIExePath();
        }

        private string FindMagicIL2CPPCLIExePath()
        {
            if(MagicIL2CPPCLIExePath == null)
            {
                var guids = AssetDatabase.FindAssets("Magic.IL2CPP.CLI");
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if(path.EndsWith(".exe"))
                    {
                        MagicIL2CPPCLIExePath = path;
                    }
                }
            }
            
            return MagicIL2CPPCLIExePath;
        }

        void OnGUI()
        {
            titleContent = new GUIContent("MAGIC Compiler");
            GUILayout.Label(new GUIContent("Class Path", "The file system paths relative to the project folder to treat as namespace roots."), EditorStyles.boldLabel);
            RenderStringListView(paths);

            GUILayout.Label("Namespaces", EditorStyles.boldLabel);
            RenderStringListView(namespaces);

            EnsureOutFolder(outFolder);

            showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true, EditorStyles.foldoutHeader);
            if(showAdvanced)
            {
                GUILayout.Label(new GUIContent("Output Folder", "The folder to write built binaries into"), EditorStyles.boldLabel);
                outFolder = GUILayout.TextField(outFolder);

                GUILayout.Label("Linking", EditorStyles.boldLabel);
                verbose = EditorGUILayout.Toggle(new GUIContent("Verbose compilation", "When true, MAGIC will write diagnostic information to the console during compilation."), verbose);
                autogenerateLinkXml = EditorGUILayout.Toggle(new GUIContent("Populate link.xml", "When true, MAGIC will automatically populate link.xml file based on the compiled namespaces. When false, you can specify the contents of link.xml below."), autogenerateLinkXml);
                GUI.enabled = !autogenerateLinkXml;
                RenderStringListView(linkXmlEntries);
                GUI.enabled = true;
            }

            if(autogenerateLinkXml)
            {
                linkXmlEntries = Directory.EnumerateFiles(outFolder)
                                    .Where(n => n.EndsWith(".dll"))
                                    .Select(n => Path.GetFileName(n).Replace(".dll", ""))
                                    .Concat(DefaultLinkXmlEntries)
                                    .ToList();
            }

            EditorGUILayout.Space(20, true);
            if (GUILayout.Button(new GUIContent("Compile", "Compile the namespaces")))
            {
                BuildLinkXml(linkXmlEntries);
                BuildNamespaces(outFolder);
            }
        }

        private void BuildLinkXml(List<string> linkXmlEntries)
        {
            var linkXml = XmlWriter.Create("Assets/link.xml");
            linkXml.WriteStartElement("linker");
            foreach (var entry in linkXmlEntries)
            {
                linkXml.WriteStartElement("assembly");
                linkXml.WriteAttributeString("fullname", entry);
                linkXml.WriteEndElement();
            }
            linkXml.WriteEndElement();
            linkXml.Close();
        }

        void EnsureOutFolder(string path = DefaultOutFolder)
        {
            if(!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        void BuildNamespaces(string buildPath = DefaultOutFolder)
        {
            if(verbose)
                Debug.LogFormat("load path {0}", string.Join(",", paths));
            foreach (var ns in namespaces)
            {
                BuildNamespace(ns);
                
            }

            var files = Directory.GetFiles(".", "*.clj.dll");
            foreach (var f in files)
            {
                Shell.MonoRun(MagicIL2CPPCLIExePath, f);
                var finalPath = Path.Combine(buildPath, Path.GetFileName(f));
                if(File.Exists(finalPath))
                {
                    File.Delete(finalPath);
                }
                File.Move(f, finalPath);
            }
        }

        private void BuildNamespace(string ns)
        {
            if(verbose)
                Debug.LogFormat("compile {0}", ns);
            MagicCompilerNamespaceVar.invoke(paths, ns);
        }
    }
}
