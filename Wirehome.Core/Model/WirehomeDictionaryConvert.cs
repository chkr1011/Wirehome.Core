using System;
using System.Collections;
using System.Reflection;
using System.Text;

namespace Wirehome.Core.Model
{
    public static class WirehomeDictionaryConvert
    {
        public static WirehomeDictionary FromObject(object source)
        {
            if (source == null)
            {
                return new WirehomeDictionary();
            }

            if (source is IDictionary dictionary)
            {
                var result = new WirehomeDictionary();
                foreach (var key in dictionary.Keys)
                {
                    result.TryAdd(Convert.ToString(key), dictionary[key]);
                }

                return result;
            }

            var result2 = new WirehomeDictionary();
            foreach (var property in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                result2[GenerateVariableName(property.Name)] = property.GetValue(source);
            }

            return result2;
        }

        public static TObject ToObject<TObject>(WirehomeDictionary dictionary) where TObject : class, new()
        {
            if (dictionary == null)
            {
                return null;
            }

            var result = Activator.CreateInstance<TObject>();

            foreach (var property in result.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var variableName = GenerateVariableName(property.Name);

                dictionary.TryGetValue(variableName, out var propertyValue);
            }

            return result;
        }

        private static string GenerateVariableName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return propertyName;
            }

            var variableName = new StringBuilder();
            foreach (var @char in propertyName)
            {
                if (char.IsUpper(@char) || char.IsNumber(@char))
                {
                    variableName.Append('_');
                }

                variableName.Append(char.ToLowerInvariant(@char));
            }

            return variableName.ToString();
        }
    }
}
