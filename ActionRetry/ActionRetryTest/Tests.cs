using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActionRetry;
using System;
using System.Threading.Tasks;

namespace ActionRetryTest
{
    [TestClass]
    public class Tests
    {
        [TestMethod]
        public async Task Trivial()
        {
            foreach (Retry.Backoff enumVal in Enum.GetValues(typeof(Retry.Backoff)))
            {
                Assert.IsTrue(new Retry(() => true, backoff: enumVal).Begin());
                Assert.IsFalse(new Retry(() => false, backoff: enumVal).Begin());

                Assert.IsTrue(await new Retry(async () =>
                {
                    await Task.Delay(0);
                    return true;
                }, backoff: enumVal).BeginASync());
                Assert.IsFalse(await new Retry(async () =>
                {
                    await Task.Delay(0);
                    return false;
                }, backoff: enumVal).BeginASync());
            }
        }
    }
}
