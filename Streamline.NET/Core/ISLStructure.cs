namespace Streamline.NET;

/// <summary>A non-polymorphic Streamline structure with a native type identifier.</summary>
public interface ISLStructure
{
    /// <summary>The type identifier stored in the native structure header.</summary>
    static abstract StructType TypeId { get; }
}
