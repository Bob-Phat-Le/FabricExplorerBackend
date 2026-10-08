using FabricExplorerBackend.Domain.Enums;
using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Commons.Models.Responses.Fabric.FabricOperation
{
    public class FabricOperationResponse
    {
        public Guid Id { get; set; }
        public string OperationType { get; set; } = null!;
        public Guid? ResourceId { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public FabricOperationStatus Status { get; set; }
        public int? PercentComplete { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
