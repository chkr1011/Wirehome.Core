using System;
using System.Reflection;
using IronPython.Runtime;
using Wirehome.Core.Python;

namespace Wirehome.Core.Model
{
    public abstract class TypedWirehomeDictionary : IPythonConvertible
    {
        public string Type { get; set; } = "success";

        protected static TModel Create<TModel>(PythonDictionary pythonDictionary) where TModel : class, new()
        {
            if (pythonDictionary == null)
            {
                return null;
            }

            var model = Activator.CreateInstance<TModel>();

            var properties = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in properties)
            {
                var key = PythonConvert.PythonfyPropertyName(property.Name);
                if (!pythonDictionary.TryGetValue(key, out var value))
                {
                    continue;
                }

                value = Convert.ChangeType(value, property.PropertyType);
                property.SetValue(model, value);
            }

            return model;
        }

        public object ConvertToPython()
        {
            return PythonConvert.ToPythonDictionary(this);
        }

        public PythonDictionary ConvertToPythonDictionary()
        {
            return PythonConvert.ToPythonDictionary(this);
        }

        public WirehomeDictionary ConvertToWirehomeDictionary()
        {
            return PythonConvert.ToPythonDictionary(this);
        }
    }
}
