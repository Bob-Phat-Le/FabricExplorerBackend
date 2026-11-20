namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing
{
    /// <summary>Open Mirroring data types a column of a delimited file can have.</summary>
    public static class FabricDataTypes
    {
        public const string Double = "Double";
        public const string Single = "Single";
        public const string Int16 = "Int16";
        public const string Int32 = "Int32";
        public const string Int64 = "Int64";
        public const string DateTime = "DateTime";
        public const string Date = "IDate";
        public const string Time = "ITime";
        public const string String = "String";
        public const string Boolean = "Boolean";
        public const string ByteArray = "ByteArray";

        public static IReadOnlyList<string> All { get; } =
            [Double, Single, Int16, Int32, Int64, DateTime, Date, Time, String, Boolean, ByteArray];

        public static bool IsSupported(string? type) => type is not null && All.Contains(type, StringComparer.Ordinal);
    }
}
