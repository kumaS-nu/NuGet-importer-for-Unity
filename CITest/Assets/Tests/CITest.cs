using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine.TestTools;

using Cysharp.Text;


namespace kumaS.NuGetImporter.CI.Tests
{
    public class CITest
    {
        [Test]
        public void NoErrorTest()
        {
            using (var sb = ZString.CreateStringBuilder())
            {
                sb.Append("foo");
                sb.AppendLine(42);
                sb.AppendFormat("{0} {1:.###}", "bar", 123.456789);
                var str = sb.ToString();
            }

            Assert.That(true);
        }
    }
}
