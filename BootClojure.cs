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
#elif UNITY_IOS
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

        /// <summary>
        /// Experimental features of the integration
        /// </summary>
        public static class Alpha
        {
            public static Func<T> GetFunc<T>(Var v)
            {
                var f = v.deref() as Magic.Function<T>;
                if (f != null)
                {
                    return new Func<T>(f.invokeTyped);
                }

                throw new ArgumentException("var " + v.ToString() + " is not a Magic.Function", "v");
            }

            public static Func<T, V> GetFunc<T, V>(string ns, string name)
            {
                return GetFunc<T, V>(Clojure.GetVar(ns, name));
            }

            public static Func<T, V> GetFunc<T, V>(Var v)
            {
                var f = v.deref() as Magic.Function<V, T>;
                if (f != null)
                {
                    return new Func<T, V>(f.invokeTyped);
                }

                throw new ArgumentException("var " + v.ToString() + " is not a Magic.Function", "v");
            }

            public static Action<T> GetAction<T>(string ns, string name)
            {
                return GetAction<T>(Clojure.GetVar(ns, name));
            }

            public static Action<T> GetAction<T>(Var v)
            {
                var f = v.deref() as Magic.Function<object, T>;
                if (f != null)
                {
                    return new Action<T>(t => f.invokeTyped(t));
                }

                throw new ArgumentException("var " + v.ToString() + " is not a Magic.Function", "v");
            }
        }
    }
}
