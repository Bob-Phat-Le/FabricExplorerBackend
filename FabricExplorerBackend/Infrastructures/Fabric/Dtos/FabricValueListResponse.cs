namespace FabricExplorerBackend.Infrastructures.Fabric.Dtos
{
    public class FabricValueListResponse<Type>
    {
        public string ContinuationToken { get; set; } = string.Empty;
        public string ContinuationUri { get; set; } = string.Empty;
        public IList<Type?> Data { get; set; }

        public FabricValueListResponse()
        {
        }

        public FabricValueListResponse(string continuationToken, string continuationUri, IList<Type?> data)
        {
            ContinuationToken = continuationToken;
            ContinuationUri = continuationUri;
            Data = data;
        }
    }
}
