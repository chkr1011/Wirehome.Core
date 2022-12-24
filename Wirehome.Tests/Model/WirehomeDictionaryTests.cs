using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Wirehome.Tests.Model
{
    [TestClass]
    public class WirehomeDictionaryTests
    {
        [TestMethod]
        public void Set_Type()
        {
            var dictionary = new Dictionary<object, object>
            {
                ["type"] = "myTestType"
            };

            Assert.AreEqual(1, dictionary.Keys.Count);
            Assert.AreEqual("myTestType", dictionary["type"]);
        }

        [TestMethod]
        public void Set_Values()
        {
            var dictionary = new Dictionary<object, object>
            {
                ["type"] = "myTestType",
                ["a"] = "b",
                ["c"] = 1,
                ["d"] = true
            };

            Assert.AreEqual(4, dictionary.Keys.Count);
            Assert.AreEqual("myTestType", dictionary["type"]);
            Assert.AreEqual("b", dictionary["a"]);
            Assert.AreEqual(1, dictionary["c"]);
            Assert.AreEqual(true, dictionary["d"]);
        }
    }
}