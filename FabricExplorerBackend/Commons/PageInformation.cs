using FabricExplorerBackend.Enums;
using System.Text.Json.Serialization;

namespace FabricExplorerBackend.Commons
{
    public class PageInformation
    {
        public PaginationMode PaginationMode { get; set; } = PaginationMode.Offset;
        public int? Total { get; set; }
        public int? Page { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage { get; set; }
        public string? NextToken { get; set; }
    }
}
