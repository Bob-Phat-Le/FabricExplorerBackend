namespace FabricExplorerBackend.Application.Configuration.Import
{
    /// <summary>Decides what to do with an entry, given whether it already exists.</summary>
    public interface IImportModeStrategy
    {
        string Name { get; }
        ImportAction Decide(bool exists);
    }

    /// <summary>Default: existing entries are left untouched.</summary>
    public sealed class MergeImportModeStrategy : IImportModeStrategy
    {
        public string Name => "merge";
        public ImportAction Decide(bool exists) => exists ? ImportAction.Skip : ImportAction.Create;
    }

    /// <summary>Existing entries take the content of the file.</summary>
    public sealed class OverwriteImportModeStrategy : IImportModeStrategy
    {
        public string Name => "overwrite";
        public ImportAction Decide(bool exists) => exists ? ImportAction.Update : ImportAction.Create;
    }
}
