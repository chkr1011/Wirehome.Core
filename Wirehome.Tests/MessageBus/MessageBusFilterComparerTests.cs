using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Wirehome.Core.MessageBus;

namespace Wirehome.Tests.MessageBus
{
    [TestClass]
    public class MessageBusFilterComparerTests
    {
        [TestMethod]
        public void Match_Wildcard()
        {
            var message = new Dictionary<object, object>
            {
                ["type"] = "myTypeX"
            };

            var filter = new Dictionary<object, object>
            {
                ["type"] = "*"
            };

            var compareResult = MessageBusFilterComparer.IsMatch(message, filter);

            Assert.IsTrue(compareResult);
        }

        [TestMethod]
        public void Match_All()
        {
        }

        [TestMethod]
        public void Match_ExistingOnly()
        {
        }
    }
}