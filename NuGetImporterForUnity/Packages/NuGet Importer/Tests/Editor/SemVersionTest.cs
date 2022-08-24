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
        public void MathExpressionTest1GraterEqual()
        {
            Assert.That(SemVer.ToMathExpression("1.0"), Is.EqualTo("1.0 <= v"));
        }

        [Test]
        public void MathExpressionTest2Grater()
        {
            Assert.That(SemVer.ToMathExpression("(1.0,)"), Is.EqualTo("1.0 < v"));
        }

        [Test]
        public void MathExpressionTest3Equal()
        {
            Assert.That(SemVer.ToMathExpression("[1.0]"), Is.EqualTo("v = 1.0"));
        }

        [Test]
        public void MathExpressionTest4LessEqual()
        {
            Assert.That(SemVer.ToMathExpression("(,1.0]"), Is.EqualTo("v <= 1.0"));
        }

        [Test]
        public void MathExpressionTest5Less()
        {
            Assert.That(SemVer.ToMathExpression("(,1.0)"), Is.EqualTo("v < 1.0"));
        }

        [Test]
        public void MathExpressionTest6GraterEqualLessEqual()
        {
            Assert.That(SemVer.ToMathExpression("[1.0,2.0]"), Is.EqualTo("1.0 <= v <= 2.0"));
        }

        [Test]
        public void MathExpressionTest7GraterLess()
        {
            Assert.That(SemVer.ToMathExpression("(1.0,2.0)"), Is.EqualTo("1.0 < v < 2.0"));
        }

        [Test]
        public void MathExpressionTest8GraterEqualLess()
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
        public void MargeTest1Same()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0]" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0]" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0]" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest2GraterEqual()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest3GraterEqual()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest4LessEqual()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(,2.0.0]" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(,1.0.0]" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,1.0.0]" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest5PreRelease()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0-alpha,2.0.0]" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0-rc.1,2.1.0]" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[1.0.0-rc.1,2.0.0]" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }


        [Test]
        public void MargeTest6PreRelease()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-alpha,2.0.0)" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-rc.1,2.1.0)" };
            var rightSemVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-rc.1,2.0.0)" };

            SemVer marged = semVer1.Marge(semVer2, false);

            Assert.That(marged.ExistVersion, Is.EqualTo(rightSemVer.ExistVersion));
            Assert.That(marged.AllowedVersion, Is.EqualTo(rightSemVer.AllowedVersion));
        }

        [Test]
        public void MargeTest7NoVersion()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-alpha,1.0.0)" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.Throws<ArgumentException>(() => semVer1.Marge(semVer2, false));
        }

        [Test]
        public void MargeTest8NoPreRelease()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-alpha,1.0.0)" };
            var semVer2 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-beta,2.0.0)" };

            Assert.Throws<ArgumentException>(() => semVer1.Marge(semVer2, true));
        }

        [Test]
        public void MargeTest9NotMatch()
        {
            var semVer1 = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-alpha,1.0.0)" };
            var semVer2 = new SemVer() { ExistVersion = new List<string>() { "1.0.0" }, AllowedVersion = "(1.0.0-beta,2.0.0)" };

            Assert.Throws<ArgumentException>(() => semVer1.Marge(semVer2, false));
        }

        #endregion

        #region Test of GetSuitVersion Suit

        [Test]
        public void GetSuitVersionTest01SuitGraterEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0-alpha" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Suit), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest02SuitGraterEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Suit), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest03SuitGraterEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0-alpha" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Suit), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest04SuitGraterEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Suit), Is.EqualTo("2.2.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest05SuitGrater()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-beta,]" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Suit), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest06SuitGraterPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-beta,]" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Suit), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest07SuitLessEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha]" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Suit), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest08SuitLessEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha]" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Suit), Is.EqualTo("2.2.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest09SuitLessPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha)" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Suit), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest10SuitNoVersion()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.2.0-alpha" };

            Assert.Throws<InvalidOperationException>(() => semVer.GetSuitVersion(true, VersionSelectMethod.Suit));
        }

        [Test]
        public void GetSuitVersionTest11SuitNoVersion()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,1.0.0)" };

            Assert.Throws<InvalidOperationException>(() => semVer.GetSuitVersion(true, VersionSelectMethod.Suit));
        }

        #endregion

        #region Test of GetSuitVersion Highest

        [Test]
        public void GetSuitVersionTest12HighestGraterEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0-alpha" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Highest), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest13HighestGraterEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Highest), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest14HighestGraterEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0-alpha" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Highest), Is.EqualTo("2.2.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest15HighestGraterEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Highest), Is.EqualTo("2.2.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest16HighestGrater()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-beta,]" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Highest), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest17HighestGraterPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-beta,]" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Highest), Is.EqualTo("2.2.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest18HighestLessEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha]" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Highest), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest19HighestLessEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,1.0.0]" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Highest), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest20HighestLessPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha)" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Highest), Is.EqualTo("2.1.1"));
        }

        [Test]
        public void GetSuitVersionTest21HighestNoVersion()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.2.0-alpha" };

            Assert.Throws<InvalidOperationException>(() => semVer.GetSuitVersion(true, VersionSelectMethod.Highest));
        }

        [Test]
        public void GetSuitVersionTest22HighestNoVersion()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,1.0.0)" };

            Assert.Throws<InvalidOperationException>(() => semVer.GetSuitVersion(true, VersionSelectMethod.Highest));
        }

        #endregion

        #region Test of GetSuitVersion Lowest

        [Test]
        public void GetSuitVersionTest23LowestGraterEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0-alpha" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Lowest), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest24LowestGraterEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Lowest), Is.EqualTo("2.0.0"));
        }

        [Test]
        public void GetSuitVersionTest25LowestGraterEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "1.0.0-alpha" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Lowest), Is.EqualTo("1.0.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest26LowestGraterEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.0.0" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Lowest), Is.EqualTo("2.0.0"));
        }

        [Test]
        public void GetSuitVersionTest27LowestGrater()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-beta,]" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Lowest), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest28LowestGraterPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0-beta,]" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Lowest), Is.EqualTo("1.0.0-beta.2"));
        }

        [Test]
        public void GetSuitVersionTest29LowestLessEqual()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha]" };

            Assert.That(semVer.GetSuitVersion(true, VersionSelectMethod.Lowest), Is.EqualTo("1.0.0"));
        }

        [Test]
        public void GetSuitVersionTest30LowestLessEqualPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,1.0.0]" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Lowest), Is.EqualTo("1.0.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest31LowestLessPreRelease()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,2.2.0-alpha)" };

            Assert.That(semVer.GetSuitVersion(false, VersionSelectMethod.Lowest), Is.EqualTo("1.0.0-alpha"));
        }

        [Test]
        public void GetSuitVersionTest32LowestNoVersion()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "2.2.0-alpha" };

            Assert.Throws<InvalidOperationException>(() => semVer.GetSuitVersion(true, VersionSelectMethod.Lowest));
        }

        [Test]
        public void GetSuitVersionTest33LowestNoVersion()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "[,1.0.0)" };

            Assert.Throws<InvalidOperationException>(() => semVer.GetSuitVersion(true, VersionSelectMethod.Lowest));
        }

        #endregion

        #region Test of IsAllowedVersion

        [Test]
        public void IsAllowedVersionTest1Low()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0, 2.1.1]" };
            Assert.IsTrue(semVer.IsAllowedVersion(new List<string>() { "2", "0", "0" }));
        }

        [Test]
        public void IsAllowedVersionTest2High()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0, 2.1.1]" };
            Assert.IsTrue(semVer.IsAllowedVersion(new List<string>() { "2", "1", "1" }));
        }

        [Test]
        public void IsAllowedVersionTest3Under()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0, 2.1.1]" };
            Assert.IsFalse(semVer.IsAllowedVersion(new List<string>() { "1", "0", "0" }));
        }

        [Test]
        public void IsAllowedVersionTest4Under()
        {
            var semVer = new SemVer() { ExistVersion = setVersion.ToList(), AllowedVersion = "(1.0.0, 2.1.1]" };
            Assert.IsFalse(semVer.IsAllowedVersion(new List<string>() { "1", "0", "0-alpha" }));
        }

        #endregion

    }
}
