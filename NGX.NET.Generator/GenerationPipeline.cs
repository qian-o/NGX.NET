namespace NGX.NET.Generator;

internal class GenerationPipeline(Models models)
{
    internal Dictionary<string, string> Generate()
    {
        Dictionary<string, string> files = [];
        TypeMapper mapper = new(models);
        ResultEmitter results = new(files);
        EnumEmitter enums = new(files);
        StructEmitter structs = new(mapper, files);
        FunctionEmitter functions = new(mapper, files, results);
        DelegateEmitter delegates = new(models, mapper, files);
        ConstantsEmitter constants = new(models, files);

        foreach ((string name, AstEnum value) in models.Enums)
        {
            enums.WriteEnum(name, value);
        }

        foreach ((string name, AstRecord value) in models.Records)
        {
            structs.WriteRecord(name, value);
        }

        foreach (IGrouping<string, AstFunction> group in models.Functions.Values.GroupBy(static function => FunctionName(function.Name).Group))
        {
            functions.WriteFunctions(group.Key, group);
        }

        delegates.WriteCallbacks();
        constants.WriteConstants();
        results.WriteResultTypes();

        return files;
    }
}
