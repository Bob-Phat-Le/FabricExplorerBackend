using FabricExplorerBackend.Domain.Enums;
using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Application.QueryService.Contracts
{
    public sealed record QueryAcceptedResponse(
        Guid QueryId,
        [property: JsonConverter(typeof(JsonStringEnumConverter))] QueryStatus Status);
}
