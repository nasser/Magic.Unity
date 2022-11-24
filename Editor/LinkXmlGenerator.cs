using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace Magic.Unity
{
    static class LinkXmlGenerator
    {
        public static void BuildLinkXml()
        {
            var cljAssemblies = AppDomain.CurrentDomain
                                        .GetAssemblies()
                                        .Where(a => a.FullName.Contains(".clj"))
                                        .Select(a => a.FullName)
                                        .Concat(new[] { "Clojure", "Magic.Runtime" });
            BuildLinkXml(cljAssemblies);

        }
        public static void BuildLinkXml(IEnumerable<string> linkXmlEntries)
        {
            Debug.Log("[Magic.Unity/LinkXmlGenerator] Generating link.xml");
            if (File.Exists("Assets/link.xml"))
                File.Delete("Assets/link.xml");
            var linkXml = XmlWriter.Create("Assets/link.xml");
            linkXml.WriteStartElement("linker");
            foreach (var entry in linkXmlEntries)
            {
                Debug.Log($"[Magic.Unity/LinkXmlGenerator] Adding entry '{entry}'");
                linkXml.WriteStartElement("assembly");
                linkXml.WriteAttributeString("fullname", entry);
                linkXml.WriteEndElement();
            }
            linkXml.WriteEndElement();
            linkXml.Close();
        }
    }
}