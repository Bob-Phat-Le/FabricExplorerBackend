using FabricExplorerBackend.Domain.Enums;

namespace FabricExplorerBackend.Commons.Models.Responses.Paginations
{
    public class PageInformation
    {
        public PaginationMode PaginationMode { get; set; } = PaginationMode.Offset;
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int ItemsCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public bool HasNextPage => Page < TotalPages;
        public bool HasPreviousPage => Page > 1 && TotalPages > 0;
    }
}
