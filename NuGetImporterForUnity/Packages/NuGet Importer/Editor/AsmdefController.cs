using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEngine;

namespace kumaS.NuGetImporter.Editor
{
    internal static class AsmdefController
    {
        internal static async Task UpdateAsmdef(IList<Package> packages, PackagePathSolverBase pathResolver)
        {
            if (NuGetImporterSettings.Instance.IsCreateAsmdefForAnalyzer)
            {
                await CreateAsmdef(packages, pathResolver);
            }
            else
            {
                await DeleteAsmdef(packages, pathResolver);
            }
        }

        private static async Task CreateAsmdef(IList<Package> packages, PackagePathSolverBase pathResolver)
        {
            var pathRequest = packages.Select(package => pathResolver.AnalyzerPath(package));
            var paths = await Task.WhenAll(pathRequest);

            foreach (var (package, path) in packages.Zip(paths, (n, p) => (n, p)))
            {
                if (path == "")
                {
                    continue;
                }

                UpdateAnalyzer(package, path);
            }
        }

        private static async Task DeleteAsmdef(IList<Package> packages, PackagePathSolverBase pathResolver)
        {
            var pathRequest = packages.Select(package => pathResolver.AnalyzerPath(package));
            var paths = await Task.WhenAll(pathRequest);

            var asmdefPath = paths.Where(path => path != "").SelectMany(path => Directory.GetFiles(path, "*-auto-gen.asmdef", SearchOption.AllDirectories)).ToList();
            var dummyPath = asmdefPath.SelectMany(path => Directory.GetFiles(Path.GetDirectoryName(path), "*dummy.cs")).ToList();

            foreach (var path in asmdefPath)
            {
                Delete(path);
            }
            foreach (var path in dummyPath)
            {
                Delete(path);
            }
        }

        private static void UpdateAnalyzer(Package package, string path)
        {
            var packageAsmdef = Directory.GetFiles(path, "*.asmdef", SearchOption.AllDirectories).Where(f => !f.EndsWith("-auto-gen.asmdef"));
            var filePath = Path.Combine(path, package.id + "-analyzer-auto-gen.asmdef");
            if (File.Exists(filePath) || packageAsmdef.Any())
            {
                return;
            }
            var adf = new AssemblyDefinitionFile();
            adf.name = package.id + ".analyzer";
            File.WriteAllText(filePath, JsonUtility.ToJson(adf));
            if (!Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories).Any())
            {
                File.WriteAllText(Path.Combine(path, Math.Abs(package.id.GetHashCode()).ToString() + "dummy.cs"), "");
            }
        }

        private static void Delete(string path)
        {
            try
            {
                File.Delete(path);
                File.Delete(path + ".meta");
            }
            catch (Exception) { }
        }
    }
}
