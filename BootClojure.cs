using UnityEngine;
using clojure.lang;

public static class BootClojure
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void Boot()
    {
        RuntimeBootstrapFlag._startDefaultServer = false;
#if UNITY_IOS
        RuntimeBootstrapFlag.CodeLoadOrder = new[] { RuntimeBootstrapFlag.CodeSource.InitType };
        RuntimeBootstrapFlag.DisableFileLoad = true;
#endif
    }
}
