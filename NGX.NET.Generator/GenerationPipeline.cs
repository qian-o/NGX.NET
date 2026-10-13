namespace NGX.NET.Generator;

internal class GenerationPipeline(AstModel model)
{
    internal Dictionary<string, string> Generate()
    {
        Dictionary<string, string> files = [];
        TypeMapper mapper = new(model);
        EnumEmitter enums = new(files);
        StructEmitter structs = new(mapper, files);
        FunctionEmitter functions = new(mapper, files);
        ConstantsEmitter constants = new(model, files);

        foreach ((string name, AstEnum value) in model.Enums)
        {
            enums.WriteEnum(name, value);
        }

        foreach ((string name, AstRecord value) in model.Records)
        {
            structs.WriteRecord(name, value);
        }

        foreach (IGrouping<string, AstFunction> group in model.Functions.Values.GroupBy(static function => GetFunctionName(function.Name).Group))
        {
            functions.WriteFunctions(group.Key, group);
        }

        constants.WriteConstants();

        return files;
    }
}
