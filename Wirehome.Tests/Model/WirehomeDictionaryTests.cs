using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wirehome.Core.Model;

namespace Wirehome.Tests.Model
{
    [TestClass]
    public class WirehomeDictionaryTests
    {
        [TestMethod]
        public void Set_Type()
        {
            var dictionary = new WirehomeDictionary().WithValue("type", "myTestType");
            
            Assert.AreEqual(1, dictionary.Keys.Count);
            Assert.AreEqual("myTestType", dictionary["type"]);
        }

        [TestMethod]
        public void Set_Values()
        {
            var dictionary = new WirehomeDictionary()
                .WithValue("type", "myTestType")
                .WithValue("a", "b")
                .WithValue("c", 1)
                .WithValue("d", true);

            Assert.AreEqual(4, dictionary.Keys.Count);
            Assert.AreEqual("myTestType", dictionary["type"]);
            Assert.AreEqual("b", dictionary["a"]);
            Assert.AreEqual(1, dictionary["c"]);
            Assert.AreEqual(true, dictionary["d"]);
        }
    }
}
