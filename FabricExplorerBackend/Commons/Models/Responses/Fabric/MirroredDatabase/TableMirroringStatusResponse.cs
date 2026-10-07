namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.MirroredDatabase
{
    public class TableMirroringStatusResponse
    {
        public string TableName { get; set; } = null!;
        public string Sql { get; set; } = null!;
        public string Target { get; set; } = null!;
        public TableMirroringStatusResponse Status { get; set; } = null!;
        public TimeSpan LastSync { get; set; }
        public TimeSpan Lag { get; set; }
    }
}
