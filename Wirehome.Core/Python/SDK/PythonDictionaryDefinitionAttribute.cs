using System;

namespace Wirehome.Core.Python.SDK
{
    public class PythonDictionaryDefinitionAttribute : Attribute
    {
        private readonly Type _type;

        public PythonDictionaryDefinitionAttribute(Type type)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
        }
    }
}
