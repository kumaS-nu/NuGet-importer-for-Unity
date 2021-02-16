#if ZIP_AVAILABLE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using kumaS.NuGetImporter.Editor.DataClasses;

using NUnit.Framework;

using UnityEngine.TestTools;

namespace kumaS.NuGetImporter.Editor.Tests
{
    public class DependencySolverTest
    {
        /*         Dependency Table
        * -------------------------------------
        * A 1.0.0       →
        * A 1.1.0-alpha →
        * A 2.0.0       →
        * -------------------------------------
        * B 1.0.0       → C 1.0.0
        * C 1.0.0       →
        * B 1.1.0-alpha → C 1.1.0-alpha
        * C 1.1.0-alpha →
        * B 2.0.0       → C 1.1.0-alpha
        * -------------------------------------
        * D 1.0.0       → E 1.0.0    F 1.0.0
        * E 1.0.0       → F 1.1.0-alpha
        * F 1.0.0-alpha →
        * D 1.1.0-alpha → E 2.0.0
        * E 1.1.0-alpha → F 1.1.0-alpha
        * E 2.0.0       →
        * F 2.0.0       →
        * -------------------------------------
        * G 1.0.0       → H 1.0.0
        * H 1.0.0       → I 1.0.0
        * I 1.0.0       → G 2.0.0
        */

        #region SetUp

        [OneTimeSetUp]
        public void SetUp()
        {
            NuGet.DeleteCache();
            NuGet.catalogCache.Add("A", MakeCatalog("A", null, null, null));
            NuGet.catalogCache.Add("B", MakeCatalog("B",
                new (string, string)[] { ("C", "1.0.0") },
                new (string, string)[] { ("C", "1.1.0-alpha") },
                new (string, string)[] { ("C", "1.1.0-alpha") }
            ));
            NuGet.catalogCache.Add("C", MakeCatalog("C", null, null, null));
            NuGet.catalogCache.Add("D", MakeCatalog("D",
                new (string, string)[] { ("E", "1.0.0"), ("F", "1.0.0") },
                new (string, string)[] { ("E", "2.0.0") },
                null
            ));
            NuGet.catalogCache.Add("E", MakeCatalog("E",
                new (string, string)[] { ("F", "1.1.0-alpha") },
                new (string, string)[] { ("F", "1.1.0-alpha") },
                null
            ));
            NuGet.catalogCache.Add("F", MakeCatalog("F", null, null, null));
            NuGet.catalogCache.Add("G", MakeCatalog("F",
                 new (string, string)[] { ("H", "1.0.0") },
                 null, null
            ));
            NuGet.catalogCache.Add("H", MakeCatalog("H",
                 new (string, string)[] { ("I", "1.0.0") },
                 null, null
            ));
            NuGet.catalogCache.Add("I", MakeCatalog("I",
                 new (string, string)[] { ("G", "2.0.0") },
                 null, null
            ));

        }

        private Catalog MakeCatalog(string id, (string id, string range)[] dependency1, (string id, string range)[] dependency1a, (string id, string range)[] dependency2)
        {
            var catalog = new Catalog
            {
                items = new Item[1]
            };
            catalog.items[0] = new Item
            {
                items = new Item1[3]
            };
            for (var i = 0; i < 3; i++)
            {
                catalog.items[0].items[i] = new Item1
                {
                    catalogEntry = new Catalogentry
                    {
                        id = id,
                        dependencyGroups = new Dependencygroup[2]
                    }
                };
                catalog.items[0].items[i].catalogEntry.dependencyGroups[0] = new Dependencygroup
                {
                    targetFramework = ".NETFramework4.6"
                };
                catalog.items[0].items[i].catalogEntry.dependencyGroups[1] = new Dependencygroup
                {
                    targetFramework = ".NETStandard2.0"
                };
            }

            catalog.items[0].items[0].catalogEntry.version = "1.0.0";
            catalog.items[0].items[1].catalogEntry.version = "1.1.0-alpha";
            catalog.items[0].items[2].catalogEntry.version = "2.0.0";

            for (var i = 0; i < 2; i++)
            {
                if (dependency1 != null)
                {
                    catalog.items[0].items[0].catalogEntry.dependencyGroups[i].dependencies = new Dependency[dependency1.Length];
                    for (var j = 0; j < dependency1.Length; j++)
                    {
                        catalog.items[0].items[0].catalogEntry.dependencyGroups[i].dependencies[j] = new Dependency
                        {
                            id = dependency1[j].id,
                            range = dependency1[j].range
                        };
                    }
                }
                else
                {
                    catalog.items[0].items[0].catalogEntry.dependencyGroups[i].dependencies = new Dependency[0];
                }

                if (dependency1a != null)
                {
                    catalog.items[0].items[1].catalogEntry.dependencyGroups[i].dependencies = new Dependency[dependency1a.Length];
                    for (var j = 0; j < dependency1a.Length; j++)
                    {
                        catalog.items[0].items[1].catalogEntry.dependencyGroups[i].dependencies[j] = new Dependency
                        {
                            id = dependency1a[j].id,
                            range = dependency1a[j].range
                        };
                    }
                }
                else
                {
                    catalog.items[0].items[1].catalogEntry.dependencyGroups[i].dependencies = new Dependency[0];
                }

                if (dependency2 != null)
                {
                    catalog.items[0].items[2].catalogEntry.dependencyGroups[i].dependencies = new Dependency[dependency2.Length];
                    for (var j = 0; j < dependency2.Length; j++)
                    {
                        catalog.items[0].items[2].catalogEntry.dependencyGroups[i].dependencies[j] = new Dependency
                        {
                            id = dependency2[j].id,
                            range = dependency2[j].range
                        };
                    }
                }
                else
                {
                    catalog.items[0].items[2].catalogEntry.dependencyGroups[i].dependencies = new Dependency[0];
                }
            }

            return catalog;

        }

        #endregion

        #region Test of FindRequiredPackages

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest01()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.0.0", true);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest02()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.0.0", false);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest03()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.1.0-alpha", false);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest04()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "2.0.0", true);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest05()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "1.0.0", true);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.0.0" };
            expected[1] = new Package { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest06()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "1.1.0-alpha", false);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest07()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", true);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest08()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", false);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest09()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", false);

            var expected = new Package[3];
            expected[0] = new Package { id = "A", version = "1.0.0" };
            expected[1] = new Package { id = "B", version = "2.0.0" };
            expected[2] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest10()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest11()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", false);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.1.0-alpha" };
            expected[2] = new Package { id = "F", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest12()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest13()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.0.0", allowedVersions = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest14()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("G", "1.0.0", true);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerExceptions.First(), Is.TypeOf(typeof(ArgumentException)));
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest15()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "F", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "F", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest16()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        #endregion

        #region Test of FindRequiredPackagesWhenChangeVersion

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest01()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "2.0.0", true);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest02()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "2.0.0", true);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest03()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "1.1.0-alpha", false);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest04()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest05()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "2.0.0", true);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest06()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest07()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest08()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("C", "2.0.0", true);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest09()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package { id = "D", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "E", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.installed.package[2] = new Package { id = "F", version = "1.0.0-alpha", allowedVersions = "1.0.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "D", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.1.0-alpha", false);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest10()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "D", version = "1.1.0-alpha", allowedVersions = "[1.0.0-alpha]" };
            PackageManager.installed.package[1] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "D", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.0.0", true);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest11()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.0.0", false);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "2.0.0" };
            expected[2] = new Package { id = "F", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest12()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "2.0.0", false);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        #endregion

        #region Test of FindRemovablePackages

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest01()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest02()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest03()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", false);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest04()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", true);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest05()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", false);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package() { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest06()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("C", false);

            var expected = new Package[0];

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest07()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("C", true);

            var expected = new Package[0];

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest08()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true);

            var expected = new Package[1];
            expected[0] = new Package() { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest09()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", false);

            var expected = new Package[1];
            expected[0] = new Package() { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest10()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", true);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest11()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", false);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest12()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "D", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "E", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "F", version = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "D", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("D", true);

            var expected = new Package[3];
            expected[0] = new Package() { id = "D", version = "1.0.0" };
            expected[1] = new Package() { id = "E", version = "1.0.0" };
            expected[2] = new Package() { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest13()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "D", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "E", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "F", version = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "D", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("D", false);

            var expected = new Package[3];
            expected[0] = new Package() { id = "D", version = "1.0.0" };
            expected[1] = new Package() { id = "E", version = "1.0.0" };
            expected[2] = new Package() { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        #endregion

        #region TearDown

        [OneTimeTearDown]
        public void TearDown()
        {
            _ = PackageManager.Initialize();
        }

        #endregion

        private void IsEqual(IEnumerable<Package> condition, IEnumerable<Package> excepted)
        {
            Assert.That(condition.Count(), Is.EqualTo(excepted.Count()));
            IEnumerable<(string id, string version)> conditionObjects = condition.Select(package => (package.id, package.version));
            (string id, string version)[] exceptedObjects = excepted.Select(package => (package.id, package.version)).ToArray();
            Assert.That(conditionObjects, Is.EquivalentTo(exceptedObjects));
        }

        private void SetNoInstalled()
        {
            PackageManager.installed = new InstalledPackages();
            PackageManager.rootPackage = new InstalledPackages();
        }

    }
}

#endif