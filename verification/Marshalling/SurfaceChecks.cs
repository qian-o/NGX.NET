namespace Marshalling;

internal static class SurfaceChecks
{
    internal static void Run()
    {
        foreach (Type type in typeof(Ngx).GetNestedTypes(BindingFlags.Public).Append(typeof(Ngx)))
        {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                SafeType(method.ReturnType);

                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    SafeType(parameter.ParameterType);
                }
            }
        }

        foreach (Type type in typeof(Ngx).Assembly.GetExportedTypes().Where(static type => type.IsValueType && !type.IsEnum))
        {
            foreach (FieldInfo field in type.GetFields())
            {
                SafeType(field.FieldType);
            }
        }

        Assert(typeof(Ngx).Assembly.GetType("NGX.NET.NGX") is null, "Old entry class remains");

        Console.WriteLine("PASS Ngx methods and public data structs expose no pointers or Native types");
    }

    private static void SafeType(Type type)
    {
        Assert(!type.IsPointer && !type.IsFunctionPointer && !type.Name.EndsWith("Native"), "Unsafe public type " + type);

        if (type.HasElementType)
        {
            SafeType(type.GetElementType()!);
        }

        foreach (Type argument in type.GenericTypeArguments)
        {
            SafeType(argument);
        }
    }
}
