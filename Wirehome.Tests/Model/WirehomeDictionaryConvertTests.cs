using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wirehome.Core.Model;

namespace Wirehome.Tests.Model
{
    [TestClass]
    public class WirehomeDictionaryConvertTests
    {
        private class TestModel
        {
            public string Variable1 { get; set; } = "var1";

            public bool TheBoolean { get; set; } = true;

            public int TheNumerousInteger { get; set; } = 47;
        }

        [TestMethod]
        public void FromObject()
        {
            var dictionary = WirehomeDictionaryConvert.FromObject(new TestModel());

            Assert.AreEqual(3, dictionary.Keys.Count);
            Assert.AreEqual("var1", dictionary["variable_1"]);
            Assert.AreEqual(true, dictionary["the_boolean"]);
            Assert.AreEqual(47, dictionary["the_numerous_integer"]);
        }

        [TestMethod]
        public void ToObject()
        {
            var source = new TestModel
            {
                TheBoolean = false,
                Variable1 = "xxx",
                TheNumerousInteger = 25
            };

            var dictionary = WirehomeDictionaryConvert.FromObject(source);

            var result = WirehomeDictionaryConvert.ToObject<TestModel>(dictionary);

            Assert.AreEqual(source.Variable1, result.Variable1);
            Assert.AreEqual(source.TheBoolean, result.TheBoolean);
            Assert.AreEqual(source.TheNumerousInteger, result.TheNumerousInteger);
        }
    }
}
