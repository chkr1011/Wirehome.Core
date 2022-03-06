using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wirehome.Core.MessageBus;

namespace Wirehome.Tests.MessageBus
{
    [TestClass]
    public class MessageBusFilterComparerTests
    {
        [TestMethod]
        public void Match_All()
        {
        }

        [TestMethod]
        public void Match_ExistingOnly()
        {
        }

        [TestMethod]
        public void Match_Wildcard()
        {
            var message = new Dictionary<string, string>
            {
                ["type"] = "myTypeX"
            };

            var filter = new Dictionary<string, string>
            {
                ["type"] = "*"
            };

            var compareResult = MessageBusFilterComparer.IsMatch(message, filter);

            Assert.IsTrue(compareResult);
        }
    }
}