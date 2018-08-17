using Microsoft.VisualStudio.TestTools.UnitTesting;
using ActionRetry;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

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

        [TestMethod]
        public async Task Exceptions_Unhandled()
        {
            foreach (Retry.Backoff enumVal in Enum.GetValues(typeof(Retry.Backoff)))
            {
                try
                {
                    new Retry(toRetry: () => throw new Exception(), backoff: enumVal).Begin();
                    Assert.Fail();
                }
                catch (Exception) { }

                try
                {
                    await new Retry(async () =>
                    {
                        await Task.Delay(0);
                        throw new Exception();
                    }, backoff: enumVal).BeginASync();
                    Assert.Fail();
                }
                catch (Exception) { }
            }
        }

        [TestMethod]
        public async Task Exceptions_Ignored()
        {
            foreach (Retry.Backoff enumVal in Enum.GetValues(typeof(Retry.Backoff)))
            {
                try
                {
                    new Retry(toRetry: () => throw new Exception(), ignoreExceptions: true,  backoff: enumVal).Begin();
                }
                catch (Exception) { Assert.Fail(); }

                try
                {
                    await new Retry(async () =>
                    {
                        await Task.Delay(0);
                        throw new Exception();
                    }, ignoreExceptions: true, backoff: enumVal).BeginASync();
                }
                catch (Exception) { Assert.Fail(); }
            }
        }

        [TestMethod]
        public async Task Exceptions_Blacklist()
        {
            foreach (Retry.Backoff enumVal in Enum.GetValues(typeof(Retry.Backoff)))
            {
                try
                {
                    new Retry(toRetry: () => throw new ArgumentException(), exceptionBlacklist: new HashSet<Type>() { typeof(ArgumentException) }, backoff: enumVal).Begin();
                }
                catch (ArgumentException) { }

                try
                {
                    await new Retry(async () =>
                    {
                        await Task.Delay(0);
                        throw new ArgumentException();
                    }, exceptionBlacklist: new HashSet<Type>() { typeof(ArgumentException) }, backoff: enumVal).BeginASync();
                }
                catch (ArgumentException) { }
            }
        }

        [TestMethod]
        public async Task Exceptions_Whitelist()
        {
            foreach (Retry.Backoff enumVal in Enum.GetValues(typeof(Retry.Backoff)))
            {
                try
                {
                    new Retry(toRetry: () => throw new ArgumentException(), exceptionWhitelist: new HashSet<Type>() { typeof(ArgumentException) }, backoff: enumVal).Begin();
                }
                catch (ArgumentException) { Assert.Fail(); }

                try
                {
                    await new Retry(async () =>
                    {
                        await Task.Delay(0);
                        throw new ArgumentException();
                    }, exceptionWhitelist: new HashSet<Type>() { typeof(ArgumentException) }, backoff: enumVal).BeginASync();
                }
                catch (ArgumentException) { Assert.Fail(); }
            }
        }
    }
}
