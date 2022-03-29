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
        * F 1.1.0-alpha →
        * D 1.1.0-alpha → E 2.0.0
        * E 1.1.0-alpha → F 1.1.0-alpha
        * E 2.0.0       →
        * F 2.0.0       →
        * -------------------------------------
        * G 1.0.0       → H 1.0.0
        * H 1.0.0       → I 1.0.0
        * I 1.0.0       → G 2.0.0
        * H 2.0.0       → I 2.0.0
        * I 2.0.0       → G 2.0.0
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
            NuGet.catalogCache.Add("G", MakeCatalog("G",
                 new (string, string)[] { ("H", "1.0.0") },
                 null, null
            ));
            NuGet.catalogCache.Add("H", MakeCatalog("H",
                 new (string, string)[] { ("I", "1.0.0") },
                 null,
                 new (string, string)[] { ("I", "2.0.0") }
            ));
            NuGet.catalogCache.Add("I", MakeCatalog("I",
                 new (string, string)[] { ("G", "2.0.0") },
                 null,
                 new (string, string)[] { ("G", "2.0.0") }
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

        #region Test of FindRequiredPackages Suit

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest01SuitZero()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest02SuitZero()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "2.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }


        [UnityTest]
        public IEnumerator FindRequiredPackagesTest03SuitZeroPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.0.0", false, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest04SuitZeroPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest05SuitOne()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "1.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.0.0" };
            expected[1] = new Package { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest06SuitOne()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest07SuitOnePreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest08SuitOnePreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest09SuitZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", false, VersionSelectMethod.Suit);

            var expected = new Package[3];
            expected[0] = new Package { id = "A", version = "1.0.0" };
            expected[1] = new Package { id = "B", version = "2.0.0" };
            expected[2] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest10SuitTwo()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest11SuitTwoPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", false, VersionSelectMethod.Suit);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.1.0-alpha" };
            expected[2] = new Package { id = "F", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest12SuitTwoPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest13SuitTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.0.0", allowedVersions = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest14SuitCircular()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("G", "1.0.0", true, VersionSelectMethod.Suit);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerExceptions.First(), Is.TypeOf(typeof(ArgumentException)));
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest15SuitNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "F", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "F", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true, VersionSelectMethod.Suit);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest16SuitNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true, VersionSelectMethod.Suit);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        #endregion

        #region Test of FindRequiredPackages Highest

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest17HighestZero()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest18HighestZero()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "2.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }


        [UnityTest]
        public IEnumerator FindRequiredPackagesTest19HighestZeroPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.0.0", false, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest20HighestZeroPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest21HighestOne()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "1.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest22HighestOne()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest23HighestOnePreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest24HighestOnePreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest25HighestZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", false, VersionSelectMethod.Highest);

            var expected = new Package[3];
            expected[0] = new Package { id = "A", version = "1.0.0" };
            expected[1] = new Package { id = "B", version = "2.0.0" };
            expected[2] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest26HighestTwo()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "2.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest27HighestTwoPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", false, VersionSelectMethod.Highest);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "2.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest28HighestTwoPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest29HighestTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.0.0", allowedVersions = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest30HighestCircular()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("G", "1.0.0", true, VersionSelectMethod.Highest);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerExceptions.First(), Is.TypeOf(typeof(ArgumentException)));
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest31HighestNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest32HighestNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true, VersionSelectMethod.Highest);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        #endregion

        #region Test of FindRequiredPackages Lowest

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest33LowestZero()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest34LowestZero()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "2.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }


        [UnityTest]
        public IEnumerator FindRequiredPackagesTest35LowestZeroPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.0.0", false, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest36LowestZeroPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("A", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest37LowestOne()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "1.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.0.0" };
            expected[1] = new Package { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest38LowestOne()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest39LowestOnePreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest40LowestOnePreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest41LowestZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("B", "2.0.0", false, VersionSelectMethod.Lowest);

            var expected = new Package[3];
            expected[0] = new Package { id = "A", version = "1.0.0" };
            expected[1] = new Package { id = "B", version = "2.0.0" };
            expected[2] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest42LowestTwo()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest43LowestTwoPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", false, VersionSelectMethod.Lowest);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.0.0" };
            expected[2] = new Package { id = "F", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest44LowestTwoPreRelease()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest45LowestTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.0.0", allowedVersions = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest46LowestSuitCircular()
        {
            SetNoInstalled();

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("G", "1.0.0", true, VersionSelectMethod.Lowest);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerExceptions.First(), Is.TypeOf(typeof(ArgumentException)));
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest47LowestNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "F", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "F", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true, VersionSelectMethod.Lowest);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesTest48LowestNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackages("D", "1.0.0", true, VersionSelectMethod.Lowest);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        #endregion

        #region Test of FindRequiredPackagesWhenChangeVersion Suit

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest01SuitZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "2.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest02SuitZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "2.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest03SuitZeroPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest04SuitOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "2.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest05SuitOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("C", "2.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest06SuitOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest07SuitOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest08SuitOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest09SuitTwo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "D", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.installed.package[1] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "D", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.0.0", true, VersionSelectMethod.Suit);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest10SuitTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package { id = "D", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "E", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.installed.package[2] = new Package { id = "F", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "D", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.1.0-alpha", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest11SuitTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.0.0", false, VersionSelectMethod.Suit);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "2.0.0" };
            expected[2] = new Package { id = "F", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest12SuitNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "2.0.0", false, VersionSelectMethod.Suit);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        #endregion

        #region Test of FindRequiredPackagesWhenChangeVersion Highest

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest13HighestZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "2.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest14HighestZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "2.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest15HighestZeroPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest16HighestOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "2.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest17HighestOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("C", "2.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest18HighestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest19HighestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest20HighestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest21HighestTwo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "D", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.installed.package[1] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "D", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.0.0", true, VersionSelectMethod.Highest);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "2.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest22HighestTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package { id = "D", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "E", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.installed.package[2] = new Package { id = "F", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "D", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.1.0-alpha", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest23HighestTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.0.0", false, VersionSelectMethod.Highest);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "2.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest24HighestNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "2.0.0", false, VersionSelectMethod.Highest);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        #endregion

        #region Test of FindRequiredPackagesWhenChangeVersion Lowest

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest25LowestZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "2.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest26LowestZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "2.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest27LowestZeroPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "A", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("A", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest28LowestOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "2.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest29LowestOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("C", "2.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "2.0.0" };
            expected[1] = new Package { id = "C", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest30LowestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest31LowestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest32LowestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "2.0.0", allowedVersions = "[2.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest33LowestTwo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "D", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };
            PackageManager.installed.package[1] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "D", version = "1.1.0-alpha", allowedVersions = "[1.1.0-alpha]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.0.0", true, VersionSelectMethod.Lowest);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "1.0.0" };
            expected[2] = new Package { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest34LowestTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package { id = "D", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "E", version = "1.0.0", allowedVersions = "1.0.0" };
            PackageManager.installed.package[2] = new Package { id = "F", version = "1.1.0-alpha", allowedVersions = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "D", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.1.0-alpha", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package { id = "D", version = "1.1.0-alpha" };
            expected[1] = new Package { id = "E", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest35LowestTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package { id = "E", version = "2.0.0", allowedVersions = "2.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("D", "1.0.0", false, VersionSelectMethod.Lowest);

            var expected = new Package[3];
            expected[0] = new Package { id = "D", version = "1.0.0" };
            expected[1] = new Package { id = "E", version = "2.0.0" };
            expected[2] = new Package { id = "F", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRequiredPackagesWhenChangeVersionTest36LowestNo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.installed.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package { id = "B", version = "1.0.0", allowedVersions = "[1.0.0]" };
            PackageManager.rootPackage.package[1] = new Package { id = "C", version = "1.0.0", allowedVersions = "[1.0.0]" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRequiredPackagesWhenChangeVersion("B", "2.0.0", false, VersionSelectMethod.Lowest);

            yield return task.AsEnumerator(false);

            Assert.That(task.Exception.InnerException, Is.TypeOf(typeof(InvalidOperationException)));
        }

        #endregion

        #region Test of FindRemovablePackages Suit

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest01SuitZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest02SuitZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest03SuitZeroPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", false, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest04SuitOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", true, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest05SuitOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("C", true, VersionSelectMethod.Suit);

            var expected = new Package[0];

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest06SuitOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package() { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest07SuitOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("C", false, VersionSelectMethod.Suit);

            var expected = new Package[0];

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest08SuitZeroOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package() { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest09SuitZeroOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", true, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest10SuitZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", false, VersionSelectMethod.Suit);

            var expected = new Package[1];
            expected[0] = new Package() { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest11SuitZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", false, VersionSelectMethod.Suit);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest12SuitTwo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "D", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "E", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "F", version = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "D", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("D", true, VersionSelectMethod.Suit);

            var expected = new Package[3];
            expected[0] = new Package() { id = "D", version = "1.0.0" };
            expected[1] = new Package() { id = "E", version = "1.0.0" };
            expected[2] = new Package() { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest13SuitTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "D", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "E", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "F", version = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "D", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("D", false, VersionSelectMethod.Suit);

            var expected = new Package[3];
            expected[0] = new Package() { id = "D", version = "1.0.0" };
            expected[1] = new Package() { id = "E", version = "1.0.0" };
            expected[2] = new Package() { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        #endregion

        #region Test of FindRemovablePackages Highest

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest14HighestZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest15HighestZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest16HighestZeroPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", false, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest17HighestOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", true, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest18HighestOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("C", true, VersionSelectMethod.Highest);

            var expected = new Package[0];

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest19HighestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package() { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest20HighestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("C", false, VersionSelectMethod.Highest);

            var expected = new Package[0];

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest21HighestZeroOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package() { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest22HighestZeroOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", true, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest23HighestZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", false, VersionSelectMethod.Highest);

            var expected = new Package[1];
            expected[0] = new Package() { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest24HighestZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", false, VersionSelectMethod.Highest);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest25HighestTwo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "D", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "E", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "F", version = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "D", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("D", true, VersionSelectMethod.Highest);

            var expected = new Package[3];
            expected[0] = new Package() { id = "D", version = "1.0.0" };
            expected[1] = new Package() { id = "E", version = "1.0.0" };
            expected[2] = new Package() { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest26HighestTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "D", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "E", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "F", version = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "D", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("D", false, VersionSelectMethod.Highest);

            var expected = new Package[3];
            expected[0] = new Package() { id = "D", version = "1.0.0" };
            expected[1] = new Package() { id = "E", version = "1.0.0" };
            expected[2] = new Package() { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        #endregion

        #region Test of FindRemovablePackages Lowest

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest27LowestZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest28LowestZero()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest29LowestZeroPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[1];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", false, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest30LowestOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", true, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest31LowestOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("C", true, VersionSelectMethod.Lowest);

            var expected = new Package[0];

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest32LowestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            expected[1] = new Package() { id = "C", version = "1.1.0-alpha" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest33LowestOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[2];
            PackageManager.installed.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };
            PackageManager.installed.package[1] = new Package() { id = "C", version = "1.1.0-alpha" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "B", version = "1.1.0-alpha" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("C", false, VersionSelectMethod.Lowest);

            var expected = new Package[0];

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest34LowestZeroOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", true, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package() { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest35LowestZeroOne()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", true, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest36LowestZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("A", false, VersionSelectMethod.Lowest);

            var expected = new Package[1];
            expected[0] = new Package() { id = "A", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest37LowestZeroOnePreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "B", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "C", version = "1.0.0" };
            PackageManager.rootPackage.package = new Package[2];
            PackageManager.rootPackage.package[0] = new Package() { id = "A", version = "1.0.0" };
            PackageManager.rootPackage.package[1] = new Package() { id = "B", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("B", false, VersionSelectMethod.Lowest);

            var expected = new Package[2];
            expected[0] = new Package() { id = "B", version = "1.0.0" };
            expected[1] = new Package() { id = "C", version = "1.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest38LowestTwo()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "D", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "E", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "F", version = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "D", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("D", true, VersionSelectMethod.Lowest);

            var expected = new Package[3];
            expected[0] = new Package() { id = "D", version = "1.0.0" };
            expected[1] = new Package() { id = "E", version = "1.0.0" };
            expected[2] = new Package() { id = "F", version = "2.0.0" };

            yield return task.AsEnumerator();

            IsEqual(task.Result, expected);
        }

        [UnityTest]
        public IEnumerator FindRemovablePackagesTest39LowestTwoPreRelease()
        {
            SetNoInstalled();
            PackageManager.installed.package = new Package[3];
            PackageManager.installed.package[0] = new Package() { id = "D", version = "1.0.0" };
            PackageManager.installed.package[1] = new Package() { id = "E", version = "1.0.0" };
            PackageManager.installed.package[2] = new Package() { id = "F", version = "2.0.0" };
            PackageManager.rootPackage.package = new Package[1];
            PackageManager.rootPackage.package[0] = new Package() { id = "D", version = "1.0.0" };

            System.Threading.Tasks.Task<List<Package>> task = DependencySolver.FindRemovablePackages("D", false, VersionSelectMethod.Lowest);

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
            PackageManager.Load();
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