using FabricExplorerBackend.Commons.Paginations;

namespace FabricExplorerBackend.Application.Configuration.Contracts
{
    public class ConfigurationItemsQuery : PaginationRequest
    {
        public string? Type { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
    }
}
