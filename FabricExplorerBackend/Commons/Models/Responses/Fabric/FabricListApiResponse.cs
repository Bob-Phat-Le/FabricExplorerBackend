namespace FabricExplorerBackend.Commons.Models.Responses.Fabric
{
    public class FabricListApiResponse<Type>
    {
        public string ContinuationToken { get; set; } = string.Empty;
        public string ContinuationUri { get; set; } = string.Empty;
        public IList<Type?> Data { get; set; }

        public FabricListApiResponse()
        {
        }

        public FabricListApiResponse(string continuationToken, string continuationUri, IList<Type?> data)
        {
            ContinuationToken = continuationToken;
            ContinuationUri = continuationUri;
            Data = data;
        }
    }
}
