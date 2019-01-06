using IronPython.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Wirehome.Core.Model;
using Wirehome.Core.Python;

namespace Wirehome.Tests.Python
{
    [TestClass]
    public class TypedWirehomeDictionaryTests
    {
        class TestModel : TypedWirehomeDictionary
        {
            public static implicit operator TestModel(PythonDictionary pythonDictionary)
            {
                return PythonConvert.CreateModel<TestModel>(pythonDictionary);
            }

            public string AString { get; set; } = "Hello";

            public int TheFirstNumber { get; set; } = 55;

            public bool AnotherBool { get; set; } = true;
        }

        [TestMethod]
        public void TypedWirehomeDictionary_To_PythonDictionary()
        {
            var model = new TestModel();
            var pythonDictionary = (PythonDictionary)model.ConvertToPython();

            Assert.AreEqual(55, pythonDictionary["the_first_number"]);
            Assert.AreEqual(true, pythonDictionary["another_bool"]);
            Assert.AreEqual("Hello", pythonDictionary["a_string"]);
        }

        [TestMethod]
        public void PythonDictionary_To_TypedWirehomeDictionary()
        {
            var pythonDictionary = new PythonDictionary
            {
                ["the_first_number"] = 33,
                ["a_string"] = "World"
                // Leave "AnotherBool" as default
            };

            TestModel model = pythonDictionary;

            Assert.AreEqual(33, model.TheFirstNumber);
            Assert.AreEqual("World", model.AString);
            Assert.AreEqual(true, model.AnotherBool);
        }
    }
}
