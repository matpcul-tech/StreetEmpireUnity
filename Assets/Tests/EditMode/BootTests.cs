using System.Text;
using NUnit.Framework;
using StreetEmpire.Boot;

namespace StreetEmpire.Tests
{
    public class BootTests
    {
        [Test]
        public void TombstoneStringsAreExtracted()
        {
            var data = new byte[] { 0x0a, 0x03 }; // protobuf noise around readable text
            var bytes = Encoding.ASCII.GetBytes("abort message: Out of memory");
            var all = new byte[data.Length + bytes.Length + 4];
            data.CopyTo(all, 0); bytes.CopyTo(all, 2); all[^2] = 0x12; all[^1] = 0x41;
            var s = BootDiagnostics.ReadableStrings(all);
            StringAssert.Contains("abort message: Out of memory", s);
            Assert.AreEqual("(empty)", BootDiagnostics.ReadableStrings(new byte[0]));
        }
    }
}
