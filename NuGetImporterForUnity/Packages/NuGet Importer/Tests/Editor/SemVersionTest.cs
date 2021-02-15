
#if ZIP_AVAILABLE

using System;
using System.Collections.Generic;
using System.Linq;

using kumaS.NuGetImporter.Editor.DataClasses;

using NUnit.Framework;

namespace kumaS.NuGetImporter.Editor.Tests
{
    public class SemVersionTest
    {
        private readonly string[] setVersion = new string[]
            {
                "1.0.0-alpha",
                "1.0.0-alpha.1",
                "1.0.0-alpha.beta",
                "1.0.0-beta",
                "1.0.0-beta.2",
                "1.0.0-beta.11",
                "1.0.0-rc.1",
                "1.0.0",
                "2.0.0",
                "2.1.0",
                "2.1.1",
                "2.2.0-alpha"
            };

        private readonly string[] rightVersion = new string[]
            {
                "2.2.0-alpha",
                "2.1.1",
                "2.1.0",
                "2.0.0",
                "1.0.0",
                "1.0.0-rc.1",
                "1.0.0-beta.11",
                "1.0.0-beta.2",
                "1.0.0-beta",
                "1.0.0-alpha.beta",
                "1.0.0-alpha.1",
                "1.0.0-alpha"
            };

#region Test of ToMathExpression

        [Test]
        public void MathExpressionTest1()
        {
            Assert.That(SemVer.ToMathExpression("1.0"), Is.EqualTo("1.0 <= v"));
        }

        [Test]
        public void MathExpressionTest2()
        {
            Assert.That(SemVer.ToMathExpression("(1.0,)"), Is.EqualTo("1.0 < v"));
        }

        [Test]
        public void MathExpressionTest3()
        {
            Assert.That(SemVer.ToMathExpression("[1.0]"), Is.EqualTo("v = 1.0"));
        }

        [Test]
        public void MathExpressionTest4()
        {
            Assert.That(SemVer.ToMathExpression("(,1.0]"), Is.EqualTo("v <= 1.0"));
        }

        [Test]
        public void MathExpressionTest5()
        {
            Assert.That(SemVer.ToMathExpression("(,1.0)"), Is.EqualTo("v < 1.0"));
        }

        [Test]
        public void MathExpressionTest6()
        {
            Assert.That(SemVer.ToMathExpression("[1.0,2.0]"), Is.EqualTo("1.0 <= v <= 2.0"));
        }

        [Test]
        public void MathExpressionTest7()
        {
            Assert.That(SemVer.ToMathExpression("(1.0,2.0)"), Is.EqualTo("1.0 < v < 2.0"));
        }

        [Test]
        public void MathExpressionTest8()
        {
            Assert.That(SemVer.ToMathExpression("[1.0,2.0)"), Is.EqualTo("1.0 <= v < 2.0"));
        }

#endregion

#region Test of SortVersion

        [Test]
        public void SortVersionTest()
        {
            Assert.That(SemVer.SortVersion(setVersion), Is.EqualTo(rightVersion));
        }

#endregion

#region Test of Marge

        [Test]
        public void MargeTest1()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0]" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0]" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0]" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest2()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest3()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest4()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(,2.0.0]" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(,1.0.0]" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,1.0.0]" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest5()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0-alpha,2.0.0]" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0-rc.1,2.1.0]" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0-rc.1,2.0.0]" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }


        [Test]
        public void MargeTest6()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-alpha,2.0.0)" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-rc.1,2.1.0)" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-rc.1,2.0.0)" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest7()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-alpha,1.0.0)" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.Throws<ArgumentException>(() => semVer1.Marge(semVer2, false));
        }

        [Test]
        public void MargeTest8()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-alpha,1.0.0)" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-beta,2.0.0)" };

            Assert.Throws<ArgumentException>(() => semVer1.Marge(semVer2, true));
        }

        [Test]
        public void MargeTest9()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-alpha,1.0.0)" };
            var semVer2 = new SemVer() { ExistVersion = new List<string>() { "1.0.0" }, AllowedVersion = "(1.0.0-beta,2.0.0)" };

            Assert.Throws<ArgumentException>(() => semVer1.Marge(semVer2, false));
        }

#endregion

#region Test of GetSuitVersion

        [Test]
        public void GetSuitVersionTest1()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0-alpha" };

            Assert.That(semVer.GetSuitVersion(false), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest2()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0-alpha" };

            Assert.That(semVer.GetSuitVersion(true), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest3()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.That(semVer.GetSuitVersion(false), Is.EqualTo("2.2.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest4()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.That(semVer.GetSuitVersion(true), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest5()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha]" };

            Assert.That(semVer.GetSuitVersion(false), Is.EqualTo("2.2.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest6()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha]" };

            Assert.That(semVer.GetSuitVersion(true), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest7()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha)" };

            Assert.That(semVer.GetSuitVersion(false), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest8()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.2.0-alpha" };

            Assert.Throws<InvalidOperationException>(() => semVer.GetSuitVersion(true));
        }

#endregion


#region Test of IsAllowedVersion

        [Test]
        public void IsAllowedVersionTest1()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0, 2.1.1]" };
            Assert.IsTrue(semVer.IsAllowedVersion(new List<string>() { "2", "0", "0" }));
        }

        [Test]
        public void IsAllowedVersionTest2()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0, 2.1.1]" };
            Assert.IsTrue(semVer.IsAllowedVersion(new List<string>() { "2", "1", "1" }));
        }

        [Test]
        public void IsAllowedVersionTest3()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0, 2.1.1]" };
            Assert.IsFalse(semVer.IsAllowedVersion(new List<string>() { "1", "0", "0" }));
        }

        [Test]
        public void IsAllowedVersionTest4()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0, 2.1.1]" };
            Assert.IsFalse(semVer.IsAllowedVersion(new List<string>() { "1", "0", "0-alpha" }));
        }

#endregion

    }
}

#endif