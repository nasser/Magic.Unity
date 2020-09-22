using System.Diagnostics;
using System.IO;
using UnityEditor;

// from arcadia
namespace Magic.Unity
{
    /// <summary>
    /// Facilities to run shell programs
    /// </summary>
    public static class Shell
    {
        public static readonly string MonoExecutablePath =
#if UNITY_EDITOR_OSX
            Path.Combine(
                EditorApplication.applicationPath,
                "Contents/MonoBleedingEdge/bin/mono");
#elif UNITY_EDITOR_WIN
            Path.Combine(
                Path.GetDirectoryName(EditorApplication.applicationPath),
                "Data/MonoBleedingEdge/bin/mono.exe");
#elif UNITY_EDITOR_LINUX
            Path.Combine(
                Path.GetDirectoryName(EditorApplication.applicationPath),
                "Data/MonoBleedingEdge/bin/mono");
#endif

        public static Process Run(string filename, string arguments = null, string workingDirectory = null)
        {
            Process process = new Process();
            process.StartInfo.FileName = filename;
            if (arguments != null) process.StartInfo.Arguments = arguments;
            if (workingDirectory != null) process.StartInfo.WorkingDirectory = workingDirectory;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.EnableRaisingEvents = true;
            process.Start();

            return process;
        }

        public static Process MonoRun(string pathToExe, string arguments, string workingDirectory)
        {
            return Run(MonoExecutablePath, pathToExe + " " + arguments, workingDirectory);
        }

        public static Process MonoRun(string pathToExe, string arguments)
        {
            return Run(MonoExecutablePath, pathToExe + " " + arguments);
        }

        public static Process MonoRun(string pathToExe)
        {
            return Run(MonoExecutablePath, pathToExe);
        }
    }
}
