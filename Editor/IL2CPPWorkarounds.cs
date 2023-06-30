using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using System.Reflection;

namespace Magic.Unity
{
    public static class IL2CPPWorkarounds
    {

        static bool IsIL2CPPEnabled()
        {
            return PlayerSettings.GetScriptingBackend(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup)) == ScriptingImplementation.IL2CPP;
        }

        public static void RewriteAssemblies()
        {
            var cljAssemblies = AppDomain.CurrentDomain
                            .GetAssemblies()
                            .Where(a => a.FullName.Contains(".clj"))
                            .Select(a => a.Location);
            RewriteAssemblies(cljAssemblies);
        }

        public static void RewriteAssemblies(IEnumerable<string> files)
        {
            GenerateGenericWorkaroundMethods.Init();
            foreach (var file in files)
            {
                RewriteAssembly(file);
            }
        }

        static void RewriteAssembly(string file)
        {
            var runtimeLocation = Path.GetDirectoryName(Assembly.Load("Magic.Runtime").Location);
            Debug.LogFormat($"[Magic.Unity] runtime location {runtimeLocation}");

            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(runtimeLocation);
            var outfile = file + ".out";
            using (var assyFile = File.OpenRead(file))
            {
                var assy = AssemblyDefinition.ReadAssembly(file, new ReaderParameters { AssemblyResolver = resolver });
                Debug.LogFormat("[Magic.Unity] processing {0} ({1})", assy.FullName, file);

                GenerateGenericWorkaroundMethods.MaybeRemoveIL2CPPWorkaround(assy);

                if(IsIL2CPPEnabled())
                {
                    Debug.LogFormat("[Magic.Unity] {0} adding IL2CPP workarounds", assy.FullName);

                    GenerateGenericWorkaroundMethods.StartRewriteAssembly(assy);
                    
                    foreach (var t in assy.MainModule.Types)
                    {
                        foreach (var m in t.Methods)
                        {
                            if (m.HasBody)
                                ProcessMethod(m);
                        }
                    }
                    
                    GenerateGenericWorkaroundMethods.FinishRewriteAssembly(assy);
                }

                assy.MainModule.Write(outfile);
            }
            File.Delete(file);
            File.Move(outfile, file);
            File.Delete(outfile);
        }

        static void ProcessMethod(MethodDefinition m)
        {
            m.Body.SimplifyMacros();

            EliminateUnreachableInstructions.RewriteMethod(m);
            GenerateGenericWorkaroundMethods.AnalyzeMethod(m);

            m.Body.OptimizeMacros();
        }
    }
}