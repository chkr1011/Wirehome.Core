using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Wirehome.Core.Extensions;

public static class Reflection
{
    public static List<Type> GetClassesAssignableFrom<TType>()
    {
        var assembly = Assembly.GetExecutingAssembly();

        return assembly.GetTypes().Where(t => t.IsClass && typeof(TType).IsAssignableFrom(t)).ToList();
    }
}