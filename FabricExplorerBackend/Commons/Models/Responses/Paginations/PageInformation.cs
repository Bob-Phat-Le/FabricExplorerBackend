using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Commons.Models.Responses.Paginations
{
    public class PageInformation
    {
        public PaginationMode PaginationMode { get; set; } = PaginationMode.Offset;
        public int TotalPages { get; set; }
        public int? TotalItems { get; set; }
        public int? Page { get; set; }
        public int PageSize { get; set; }
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1 && Page <= TotalPages;
        public string? NextToken { get; set; }
    }
}
