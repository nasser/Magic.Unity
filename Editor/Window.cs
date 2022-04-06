using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        [SerializeField] bool showOptimizations = false;
        [SerializeField] bool verbose = false;
        [SerializeField] bool stronglyTypedInvokes = true;
        [SerializeField] bool directLinking = true;
        [SerializeField] bool elideMeta = false;
        [SerializeField] bool legacyDynamicCallsites = false;
        [SerializeField] bool emitIL2CPPWorkaround = true;

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

        static Var MagicFlagsStronglyTypedInvokes;
        static Var MagicFlagsDirectLinking;
        static Var MagicFlagsElideMeta;
        static Var MagicFlagsLegacyDynamicCallsites;
        static Var MagicFlagsEmitIL2CPPWorkaround;
        static Var MagicCompilerNamespaceVar;
        static Var ClojureLoadPathsVar;
        static string MagicIL2CPPCLIExePath = null;

        static void BootClojure()
        {
            var assemblyPath = Path.GetDirectoryName(Assembly.Load("Clojure").Location);
			foreach(var cljDll in Directory.EnumerateFiles(assemblyPath, "*.clj.dll"))
			{
				Assembly.LoadFile(cljDll);
			}

			RT.Initialize(doRuntimePostBoostrap: false);
			RT.TryLoadInitType("clojure/core");
			RT.TryLoadInitType("magic/api");

			RT.var("clojure.core", "*load-fn*").bindRoot(RT.var("clojure.core", "-load"));
			RT.var("clojure.core", "*eval-form-fn*").bindRoot(RT.var("magic.api", "eval"));
			RT.var("clojure.core", "*load-file-fn*").bindRoot(RT.var("magic.api", "runtime-load-file"));
			RT.var("clojure.core", "*compile-file-fn*").bindRoot(RT.var("magic.api", "runtime-compile-file"));
			RT.var("clojure.core", "*macroexpand-1-fn*").bindRoot(RT.var("magic.api", "runtime-macroexpand-1"));

            MagicFlagsStronglyTypedInvokes = RT.var("magic.flags", "*strongly-typed-invokes*");
            MagicFlagsDirectLinking = RT.var("magic.flags", "*direct-linking*");
            MagicFlagsElideMeta = RT.var("magic.flags", "*elide-meta*");
            MagicFlagsLegacyDynamicCallsites = RT.var("magic.flags", "*legacy-dynamic-callsites*");
            MagicFlagsEmitIL2CPPWorkaround = RT.var("magic.flags", "*emit-il2cpp-workaround*");
        }

        void OnEnable()
        {
            BootClojure();
            UnityEngine.Debug.LogFormat("Clojure {0}", RT.var("clojure.core", "clojure-version").invoke());
            UnityEngine.Debug.LogFormat("MAGIC {0}", RT.var("magic.api", "version").deref());
            MagicCompilerNamespaceVar = RT.var("magic.api", "compile-namespace");
            ClojureLoadPathsVar = RT.var("clojure.core", "*load-paths*");
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

        List<string> GetXmlEntries() {
            return Directory.EnumerateFiles(outFolder)
                            .Where(n => n.EndsWith(".dll"))
                            .Select(n => Path.GetFileName(n).Replace(".dll", ""))
                            .Concat(DefaultLinkXmlEntries)
                            .ToList();
        }

        void OnGUI()
        {
            titleContent = new GUIContent("MAGIC Compiler");
            GUILayout.Label(new GUIContent("Class Path", "The file system paths relative to the project folder to treat as namespace roots."), EditorStyles.boldLabel);
            RenderStringListView(paths);

            GUILayout.Label("Namespaces", EditorStyles.boldLabel);
            RenderStringListView(namespaces);

            EnsureOutFolder(outFolder);

            showOptimizations = EditorGUILayout.Foldout(showOptimizations, "Optimizations", true, EditorStyles.foldoutHeader);
            if(showOptimizations)
            {
                stronglyTypedInvokes = EditorGUILayout.Toggle(new GUIContent("Hinted Invocations", "When possible, invoke type hinted Clojure functions through strongly typed invocation interfaces. Avoids boxing value types."), stronglyTypedInvokes);
                directLinking = EditorGUILayout.Toggle(new GUIContent("Direct Linking", "When possible, invoke type hinted Clojure functions as static methods. Avoids boxing value types and enables funcion inlining."), directLinking);
                elideMeta = EditorGUILayout.Toggle(new GUIContent("Elide Metadata", "Do not emit metadata. Avoid allocations when creating anonymous functions, but breaks Clojure features that depend on metadata like records."), elideMeta);
                legacyDynamicCallsites = EditorGUILayout.Toggle(new GUIContent("Legacy Dynamic Callsites", "Use legacy reflection-based dynamic callsites instead. Only enable for debugging. Dynamic call sites up to 30x slower when turned on."), legacyDynamicCallsites);
                emitIL2CPPWorkaround = EditorGUILayout.Toggle(new GUIContent("Emit IL2CPP Workaround", "Generate bytecode to work around IL2CPP limitations. Must be turned on for MAGIC to work on IL2CPP targets."), emitIL2CPPWorkaround);
            }

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
                linkXmlEntries = GetXmlEntries();
                BuildLinkXml(linkXmlEntries);
            }

            EditorGUILayout.Space(20, true);
            if (GUILayout.Button(new GUIContent("Compile", "Compile the namespaces")))
            {
                BuildNamespaces(outFolder);
                if(autogenerateLinkXml)
                {
                    linkXmlEntries = GetXmlEntries();
                    BuildLinkXml(linkXmlEntries);
                }
            }
        }

        private void BuildLinkXml(List<string> linkXmlEntries)
        {
            if(File.Exists("Assets/link.xml"))
                File.Delete("Assets/link.xml");
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
            var options = RT.mapUniqueKeys(RT.keyword(null, "write-files"), true);
            Var.pushThreadBindings(RT.mapUniqueKeys(
                MagicFlagsStronglyTypedInvokes, stronglyTypedInvokes,
                MagicFlagsDirectLinking, directLinking,
                MagicFlagsElideMeta, elideMeta,
                MagicFlagsLegacyDynamicCallsites, legacyDynamicCallsites,
                MagicFlagsEmitIL2CPPWorkaround, emitIL2CPPWorkaround,
                ClojureLoadPathsVar, paths
            ));
            try
            {
                if(verbose)
                {
                    Debug.LogFormat("compile {0}", ns);
                    Debug.LogFormat("{0} {1}", MagicFlagsStronglyTypedInvokes, MagicFlagsStronglyTypedInvokes.deref());
                    Debug.LogFormat("{0} {1}", MagicFlagsDirectLinking, MagicFlagsDirectLinking.deref());
                    Debug.LogFormat("{0} {1}", MagicFlagsElideMeta, MagicFlagsElideMeta.deref());
                    Debug.LogFormat("{0} {1}", MagicFlagsLegacyDynamicCallsites, MagicFlagsLegacyDynamicCallsites.deref());
                    Debug.LogFormat("{0} {1}", MagicFlagsEmitIL2CPPWorkaround, MagicFlagsEmitIL2CPPWorkaround.deref());
                    Debug.LogFormat("{0} {1}", ClojureLoadPathsVar, ClojureLoadPathsVar.deref());
                }
                MagicCompilerNamespaceVar.invoke(ns, options);
            }
            finally
            {
                Var.popThreadBindings();
            }
        }
    }
}
