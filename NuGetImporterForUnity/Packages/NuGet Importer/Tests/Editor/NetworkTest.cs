using System.Collections;
using System.Collections.Generic;
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
            var task = NuGet.SearchPackage();
            yield return task.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator GetCatalog()
        {
            var task = NuGet.SearchPackage();
            yield return task.AsEnumerator();
            var task1 = NuGet.GetCatalog(task.Result.data[0].id);
            yield return task1.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator GetIcon()
        {
            var task = NuGet.SearchPackage();
            yield return task.AsEnumerator();
            var task1 = PackageDataExtentionToGUI.GetIcon(task.Result.data[0]);
            yield return task1.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator ChangeTimeout1()
        {
            NuGetImporterSettings.Instance.Timeout = 100;
            var task = Task.Delay(1000);
            yield return task.AsEnumerator();
            var task1 = NuGet.SearchPackage();
            yield return task1.AsEnumerator();
            NuGetImporterSettings.Instance.Timeout = 300;
            var task2 = Task.Delay(1000);
            yield return task2.AsEnumerator();
            var task3 = NuGet.GetCatalog(task1.Result.data[0].id);
            yield return task3.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator ChangeTimeout2()
        {
            NuGetImporterSettings.Instance.Timeout = 100;
            var task = Task.Delay(1000);
            yield return task.AsEnumerator();
            var task1 = NuGet.SearchPackage();
            yield return task1.AsEnumerator();
            NuGetImporterSettings.Instance.Timeout = 300;
            var task2 = NuGet.GetCatalog(task1.Result.data[0].id);
            yield return task2.AsEnumerator();
            Assert.That(true);
        }

        [UnityTest]
        public IEnumerator ChangeTimeout3()
        {
            NuGetImporterSettings.Instance.Timeout = 100;
            var task = Task.Delay(1000);
            yield return task.AsEnumerator();
            var task1 = NuGet.SearchPackage();
            NuGetImporterSettings.Instance.Timeout = 300;
            yield return task1.AsEnumerator();
            var task2 = NuGet.GetCatalog(task1.Result.data[0].id);
            yield return task2.AsEnumerator();
            Assert.That(true);
        }
    }
}
