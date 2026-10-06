using Azure;
using FabricExplorerBackend.Commons.Models.Requests.FabricOperation;
using FabricExplorerBackend.Features.Fabric.Operation;
using Microsoft.Fabric.Api.Utils;
using Riok.Mapperly.Abstractions;

namespace FabricExplorerBackend.Mappers.FabricOperation
{
    [Mapper]
    public partial class FabricOperationMapper : IFabricOperationMapper
    {
        [MapPropertyFromSource(nameof(CreateFabricOperationRequest.OperationId), Use = nameof(GetOperationId))]
        [MapPropertyFromSource(nameof(CreateFabricOperationRequest.OperationUrl), Use = nameof(GetOperationUrl))]
        [MapPropertyFromSource(nameof(CreateFabricOperationRequest.RetryAfter), Use = nameof(GetRetryAfter))]
        public partial CreateFabricOperationRequest Map(Response response);

        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.LastPolledAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.NextPollAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.PercentComplete))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.CreatedAt))]
        [MapperIgnoreTarget(nameof(Domain.Entities.FabricOperation.UpdatedAt))]
        public partial Domain.Entities.FabricOperation Map(CreateFabricOperationRequest request);

        private Guid GetOperationId(Response response)
        {
            return Guid.Parse(response.GetXmsOperationIdHeader());
        }
        private Uri GetOperationUrl(Response response)
        {
            return new Uri(response.GetLocationHeader());
        }
        private TimeSpan GetRetryAfter(Response response)
        {
            return TimeSpan.FromSeconds((double)response.GetRetryAfterHeader()!);
        }
    }
}
