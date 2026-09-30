using System.Text.Json;

namespace NGX.NET.Generator;

internal sealed partial class Emitter
{
    private string Type(JsonElement type)
    {
        string kind = type.Text("kind");
        string cpp = type.Text("cpp").Replace("const ", "", StringComparison.Ordinal).Trim();

        if (cpp == "size_t")
        {
            return "nuint";
        }

        if (cpp == "size_t *")
        {
            return "nuint*";
        }

        if (kind is "POINTER" or "LVALUEREFERENCE" or "RVALUEREFERENCE")
        {
            JsonElement element = type.GetProperty("element");
            string elementKind = element.Text("kind");

            if (elementKind is "FUNCTIONPROTO" or "FUNCTIONNOPROTO")
            {
                return "delegate* unmanaged[Cdecl]<" + string.Join(", ", element.Items("arguments").Select(Type).Append(Type(element.GetProperty("result")))) + ">";
            }

            if (elementKind == "RECORD" && !element.Text("name").StartsWith("NVSDK_NGX_", StringComparison.Ordinal) && records[element.Text("name")].GetProperty("opaque").GetBoolean())
            {
                return "nint";
            }

            return (elementKind == "WCHAR" ? "void" : Type(element)) + "*";
        }

        if (kind is "ENUM" or "RECORD")
        {
            return unionNames.GetValueOrDefault(type.Text("name"), TypeName(type.Text("name")));
        }

        return kind switch
        {
            "VOID" => "void",
            "BOOL" => "NGXBool8",
            "CHAR_S" or "CHAR_U" or "SCHAR" => "sbyte",
            "UCHAR" => "byte",
            "SHORT" => "short",
            "USHORT" => "ushort",
            "INT" => "int",
            "UINT" => "uint",
            "LONG" => type.Number("size") == 8 ? "long" : "int",
            "ULONG" => type.Number("size") == 8 ? "ulong" : "uint",
            "LONGLONG" => "long",
            "ULONGLONG" => "ulong",
            "FLOAT" => "float",
            "DOUBLE" => "double",
            "WCHAR" => throw new InvalidOperationException("A wchar_t value requires a reviewed platform adaptation."),
            _ => throw new InvalidOperationException($"Unsupported native type: {type}")
        };
    }

    // These fields have explicit vector/matrix semantics in the SDK. Array
    // length alone is not sufficient to map arbitrary native data to math types.
    private static string? MathFieldType(string record, JsonElement field)
    {
        string? managed = record switch
        {
            "NVSDK_NGX_DLSSG_Opt_Eval_Params" => field.Text("name") switch
            {
                "cameraViewToClip" or "clipToCameraView" or "clipToLensClip" or "clipToPrevClip" or "prevClipToClip" => "Matrix4x4",
                "jitterOffset" or "mvecScale" or "cameraPinholeOffset" => "Vector2",
                "cameraPos" or "cameraUp" or "cameraRight" or "cameraFwd" => "Vector3",
                _ => null
            },
            "NVSDK_NGX_CUDA_DLSSD_Eval_Params" or "NVSDK_NGX_D3D11_DLSSD_Eval_Params" or
            "NVSDK_NGX_D3D12_DLSSD_Eval_Params" or "NVSDK_NGX_VK_DLSSD_Eval_Params"
                when field.Text("name") is "pInWorldToViewMatrix" or "pInViewToClipMatrix" => "Matrix4x4*",
            _ => null
        };

        if (managed is null)
        {
            return null;
        }

        JsonElement type = field.GetProperty("type");
        bool compatible = managed switch
        {
            "Vector2" => IsFloatArray(type, 2),
            "Vector3" => IsFloatArray(type, 3),
            "Matrix4x4" => IsFloatArray(type, 4, 4),
            "Matrix4x4*" => type.Text("kind") == "POINTER" && type.GetProperty("element").Text("kind") == "FLOAT" && type.GetProperty("element").Number("size") == sizeof(float),
            _ => false
        };

        if (!compatible)
        {
            throw new InvalidOperationException($"A mathematical field mapping requires review: {record}::{field.Text("name")} -> {managed}");
        }

        return managed;
    }

    private static bool IsFloatArray(JsonElement type, params int[] dimensions)
    {
        foreach (int count in dimensions)
        {
            if (type.Text("kind") != "CONSTANTARRAY" || type.Number("count") != count)
            {
                return false;
            }

            type = type.GetProperty("element");
        }

        return type.Text("kind") == "FLOAT" && type.Number("size") == sizeof(float);
    }
}
