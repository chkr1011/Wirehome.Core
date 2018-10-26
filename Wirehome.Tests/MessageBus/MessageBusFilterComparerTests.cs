using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;

namespace Wirehome.Tests.MessageBus
{
    [TestClass]
    public class MessageBusFilterComparerTests
    {
        [TestMethod]
        public void Match_Wildcard()
        {
            var message = new WirehomeDictionary()
                .WithValue("type", "myTypeX");

            var filter = new WirehomeDictionary()
                .WithValue("type", "*");

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