using System;
using clojure.lang;

namespace Magic.Unity
{
    /// <summary>
    /// MAGIC's Unity-specific Clojure integration
    /// </summary>
    public static class Clojure
    {
        static bool _booted = false;
        static Var RequireVar;

        /// <summary>
        /// Initialize the Clojure runtime.
        /// </summary>
        /// <remarks>
        /// This must be run before any Clojure functions can run. The
        /// Magic.Unity integration functions do this automatically.
        /// </remarks>
        public static void Boot()
        {
            if (!_booted)
            {
                _booted = true;
                RuntimeBootstrapFlag._startDefaultServer = false;
                RuntimeBootstrapFlag.SkipSpecChecks = true;
#if UNITY_EDITOR
                RuntimeBootstrapFlag.CodeLoadOrder = new[] {
                    RuntimeBootstrapFlag.CodeSource.InitType,
                    RuntimeBootstrapFlag.CodeSource.FileSystem,
                    RuntimeBootstrapFlag.CodeSource.EmbeddedResource };
#elif ENABLE_IL2CPP
                RuntimeBootstrapFlag.CodeLoadOrder = new[] { RuntimeBootstrapFlag.CodeSource.InitType };
                RuntimeBootstrapFlag.DisableFileLoad = true;
                RuntimeBootstrapFlag._doRTPostBootstrap = false;
#endif
                RequireVar = RT.var("clojure.core", "require");
            }
        }

        /// <summary>
        /// Lookup a Clojure var
        /// </summary>
        /// <param name="ns">The namespace of the var</param>
        /// <param name="name">The name of the var</param>
        /// <returns></returns>
        public static Var GetVar(string ns, string name)
        {
            Boot();
            return RT.var(ns, name);
        }

        /// <summary>
        /// Require a Clojure namespace
        /// </summary>
        /// <param name="ns">The name of the namespace</param>
        public static void Require(string ns)
        {
            Boot();
            RequireVar.invoke(Symbol.intern(ns));
        }
    }
}
