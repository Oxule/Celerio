namespace CelerioSamples;

using Celerio;
using static Celerio.Result;

public enum SampleEnum
{
    Option1,
    Option2,
    Option3
}

public class SampleObject
{
    public string Name { get; set; }
    public int Value { get; set; }
}

public static class Parameters
{
    [Get("/params/query/string")]
    public static Result Query_String(string a)
    {
        return Ok().Text(a);
    }
    [Get("/params/query/int")]
    public static Result Query_Int(int a)
    {
        return Ok().Text(a);
    }

    // String variations
    [Get("/params/query/string/nullable")]
    public static Result Query_String_Nullable(string? a = null)
    {
        return Ok().Text(a ?? "null");
    }

    [Get("/params/query/string/default")]
    public static Result Query_String_Default(string a = "default_value")
    {
        return Ok().Text(a);
    }

    // Int variations
    [Get("/params/query/int/nullable")]
    public static Result Query_Int_Nullable(int? a = null)
    {
        return Ok().Text(a?.ToString() ?? "null");
    }

    [Get("/params/query/int/default")]
    public static Result Query_Int_Default(int a = 100)
    {
        return Ok().Text(a.ToString());
    }

    // Object variations
    [Get("/params/query/object")]
    public static Result Query_Object(SampleObject a)
    {
        return Ok().Json(a);
    }

    [Get("/params/query/object/nullable")]
    public static Result Query_Object_Nullable(SampleObject? a = null)
    {
        return Ok().Json(a ?? new SampleObject { Name = "default", Value = 0 });
    }

    [Get("/params/query/object/default")]
    public static Result Query_Object_Default(SampleObject a = null!)
    {
        return Ok().Json(a ?? new SampleObject { Name = "default", Value = 42 });
    }

    // Enum variations
    [Get("/params/query/enum")]
    public static Result Query_Enum(SampleEnum a)
    {
        return Ok().Text(a.ToString());
    }

    [Get("/params/query/enum/nullable")]
    public static Result Query_Enum_Nullable(SampleEnum? a = null)
    {
        return Ok().Text(a?.ToString() ?? "null");
    }

    [Get("/params/query/enum/default")]
    public static Result Query_Enum_Default(SampleEnum a = SampleEnum.Option2)
    {
        return Ok().Text(a.ToString());
    }

    // Array variations
    [Get("/params/query/int/array")]
    public static Result Query_Int_Array(int[] a)
    {
        return Ok().Text($"[{string.Join(';', a)}]");
    }

    [Get("/params/query/string/array")]
    public static Result Query_String_Array(string[] a)
    {
        return Ok().Text($"[{string.Join(';', a)}]");
    }

    [Get("/params/query/enum/array")]
    public static Result Query_Enum_Array(SampleEnum[] a)
    {
        return Ok().Text($"[{string.Join(';', a)}]");
    }

    [Get("/params/query/object/array")]
    public static Result Query_Object_Array(SampleObject[] a)
    {
        return Ok().Json(a);
    }

    [Post("/params/body/string")]
    public static Result Body_String(string body)
    {
        return Ok().Text($"Body: {body}");
    }

    [Post("/params/body/bytes")]
    public static Result Body_Bytes(byte[] body)
    {
        return Ok().Text($"Body length: {body.Length}");
    }

    [Post("/params/body/object")]
    public static Result Body_Object(SampleObject body)
    {
        return Ok().Json(body);
    }
}
