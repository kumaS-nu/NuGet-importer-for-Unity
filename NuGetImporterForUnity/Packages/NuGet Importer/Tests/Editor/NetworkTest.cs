using System.Collections;
using System.Threading.Tasks;

using NUnit.Framework;

using UnityEngine.TestTools;

namespace kumaS.NuGetImporter.Editor.Tests
{
    public class NetworkTest
    {
        [UnityTest]
        public IEnumerator SearchPackage()
        {
            Task<DataClasses.SearchResult> task = NuGet.SearchPackage();
            yield return task.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator GetCatalog()
        {
            Task<DataClasses.SearchResult> task = NuGet.SearchPackage();
            yield return task.AsEnumerator();
            Task<DataClasses.Catalog> task1 = NuGet.GetCatalog(task.Result.data[0].id);
            yield return task1.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator GetIcon()
        {
            Task<DataClasses.SearchResult> task = NuGet.SearchPackage();
            yield return task.AsEnumerator();
            Task task1 = PackageDataExtentionToGUI.GetIcon(task.Result.data[0]);
            yield return task1.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator ChangeTimeout1()
        {
            NuGetImporterSettings.Instance.Timeout = 100;
            var task = Task.Delay(1000);
            yield return task.AsEnumerator();
            Task<DataClasses.SearchResult> task1 = NuGet.SearchPackage();
            yield return task1.AsEnumerator();
            NuGetImporterSettings.Instance.Timeout = 300;
            var task2 = Task.Delay(1000);
            yield return task2.AsEnumerator();
            Task<DataClasses.Catalog> task3 = NuGet.GetCatalog(task1.Result.data[0].id);
            yield return task3.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator ChangeTimeout2()
        {
            NuGetImporterSettings.Instance.Timeout = 100;
            var task = Task.Delay(1000);
            yield return task.AsEnumerator();
            Task<DataClasses.SearchResult> task1 = NuGet.SearchPackage();
            yield return task1.AsEnumerator();
            NuGetImporterSettings.Instance.Timeout = 300;
            Task<DataClasses.Catalog> task2 = NuGet.GetCatalog(task1.Result.data[0].id);
            yield return task2.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator ChangeTimeout3()
        {
            NuGetImporterSettings.Instance.Timeout = 100;
            var task = Task.Delay(1000);
            yield return task.AsEnumerator();
            Task<DataClasses.SearchResult> task1 = NuGet.SearchPackage();
            NuGetImporterSettings.Instance.Timeout = 300;
            yield return task1.AsEnumerator();
            Task<DataClasses.Catalog> task2 = NuGet.GetCatalog(task1.Result.data[0].id);
            yield return task2.AsEnumerator();
            Assert.That(true);
        }
    }
}
