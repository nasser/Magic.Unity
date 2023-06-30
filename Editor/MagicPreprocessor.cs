using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Magic.Unity
{
    class BuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            Debug.Log($"[Magic.Unity] preprocessing build at path {report.summary.outputPath} ({report.summary.platform})");
            foreach (var file in report.files)
            {
                Debug.Log($"[Magic.Unity] file {file.path}");
            }

            try
            {
                IL2CPPWorkarounds.RewriteAssemblies();
                LinkXmlGenerator.BuildLinkXml();
            } catch (Exception e)
            {
                throw new BuildPlayerWindow.BuildMethodException(e.Message);
            }
        }
    }
}
