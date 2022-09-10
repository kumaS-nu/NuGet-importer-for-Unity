using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Unity.CodeEditor;

using UnityEditor;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    /// <summary>
    /// <para>Class for configuring Roslyn Analyzer plugins.</para>
    /// <para>Roslyn Analyzerの設定をするためのクラス。</para>
    /// </summary>
    public class NuGetAnalyzerImportSetting : AssetPostprocessor
    {
#pragma warning disable CS0162 // 到達できないコードが検出されました
        public static bool HasAnalyzerSupport
        {
            get
            {
#if UNITY_2020_2_OR_NEWER
                return true;
#endif
                System.Type codeEditorType = CodeEditor.CurrentEditor.GetType();
                if (codeEditorType.Name == "VSCodeScriptEditor")
                {
#if HAS_ROSLYN_ANALZYER_SUPPORT_VSCODE
                    return true;
#endif
                }

                if (codeEditorType.Name == "RiderScriptEditor")
                {
#if HAS_ROSLYN_ANALZYER_SUPPORT_RIDER
                    return true;
#endif
                }
                return false;
            }
        }
#pragma warning restore CS0162 // 到達できないコードが検出されました

        private static readonly Regex rx = new Regex(@"[/\\]dotnet[/\\]cs[/\\]", RegexOptions.IgnoreCase);

        private static bool IsAnalyzer(string path)
        {
            return rx.IsMatch(path);
        }

        private void OnPreprocessAsset()
        {
            if (!assetImporter.importSettingsMissing)
            {
                return;
            }

            var pluginImporter = assetImporter as PluginImporter;
            if (pluginImporter == null)
            {
                return;
            }
            if (!IsAnalyzer(assetImporter.assetPath))
            {
                return;
            }

            pluginImporter.SetCompatibleWithAnyPlatform(false);
            pluginImporter.SetCompatibleWithEditor(false);
        }

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            AssetDatabase.StartAssetEditing();
            var isChanged = false;
            foreach (var importedAsset in importedAssets)
            {
                if (!importedAsset.EndsWith(".dll") || !IsAnalyzer(importedAsset))
                {
                    continue;
                }
                Object asset = AssetDatabase.LoadMainAssetAtPath(importedAsset);
                AssetDatabase.SetLabels(asset, new[] { "RoslynAnalyzer" });
                isChanged = true;
            }

            foreach (var deletedAsset in deletedAssets)
            {
                if (deletedAsset.EndsWith(".dll") && IsAnalyzer(deletedAsset))
                {
                    isChanged = true;
                    break;
                }
            }

            // Explicitly update project files since they are not automatically updated.
            if (isChanged)
            {
                CodeEditor.CurrentEditor.SyncAll();
            }

            AssetDatabase.StopAssetEditing();
        }

        private static string OnGeneratedCSProject(string path, string content)
        {
            if (CodeEditor.CurrentEditor.GetType().Name == "VSCodeScriptEditor" && HasAnalyzerSupport)
            {
                // Analyzer is automatically added in VSCode.
                return content;
            }

            NuGetImporterSettings.EnsureSetProjectSettingsPath();

            var packageDir = NuGetImporterSettings.Instance.InstallMethod == DataClasses.InstallMethod.AsAssets ? Path.Combine(Application.dataPath, "Packages") : Application.dataPath.Replace("Assets", "Packages");
            var analyzersPath = Directory.EnumerateFiles(packageDir, "*.dll", SearchOption.AllDirectories).Where(p => IsAnalyzer(p))
                                         .Select(p => p.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)).ToArray();
            var xDoc = XDocument.Parse(content);
            XElement project = xDoc.Root;
            XNamespace xNamespace = project.Name.Namespace;

            var baseDir = Path.GetDirectoryName(path);
            IEnumerable<XElement> analyzer = project.Descendants(xNamespace + "Analyzer");
            var analyzersInCsproj = new HashSet<string>(analyzer.Select(x => x.Attribute("Include")?.Value).Where(x => x != null));
            IEnumerable<string> addingAnalyzer = analyzersPath.Where(x => !analyzersInCsproj.Contains(x));
            if (!addingAnalyzer.Any())
            {
                return content;
            }

            XElement analyzerGroup;
            if (analyzer.Any())
            {
                analyzerGroup = analyzer.First().Parent;
            }
            else
            {
                analyzerGroup = new XElement("ItemGroup");
                project.Add(analyzerGroup);
            }
            analyzerGroup.Add(addingAnalyzer.Select(x => new XElement(xNamespace + "Analyzer", new XAttribute("Include", x))));
            content = xDoc.ToString();
            return content;
        }
    }
}
