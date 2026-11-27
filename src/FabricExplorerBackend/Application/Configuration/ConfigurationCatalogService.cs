using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Handlers;
using FabricExplorerBackend.Application.Configuration.Import;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Commons.Security;
using FabricExplorerBackend.Infrastructures.Options;
using Microsoft.Extensions.Options;

namespace FabricExplorerBackend.Application.Configuration
{
    public sealed class ConfigurationCatalogService(
        IConfigurationHandlerRegistry registry,
        IEnumerable<IImportModeStrategy> modes,
        ICurrentUser currentUser,
        IOptions<ConfigurationTransferOptions> options) : IConfigurationCatalogService
    {
        public Result<ConfigurationCapabilitiesResponse> GetCapabilities()
        {
            var limits = options.Value;

            return Result<ConfigurationCapabilitiesResponse>.Success(new ConfigurationCapabilitiesResponse(
                ConfigurationFormat.Name,
                limits.SupportedFormatVersions,
                limits.MaxFileSizeBytes,
                limits.MaxEntriesPerType,
                registry.Ascending.Select(h => new ConfigurationTypeInfo(h.Type, h.Collection)).ToList(),
                modes.Select(m => m.Name).ToList()));
        }

        /// <summary>
        /// One inventory over every entry type. The types are merged here so filtering, sorting and
        /// paging apply to the whole list, not to each type separately.
        /// </summary>
        public async Task<Result<PagedResponse<IReadOnlyList<ConfigurationItemResponse>>>> GetItemsAsync(
            ConfigurationItemsQuery query, CancellationToken cancellationToken = default)
        {
            query.Validate();

            var all = new List<ConfigurationItemResponse>();
            foreach (var handler in registry.Ascending)
            {
                // if query is not null or whitespace, but the handler type is not equal to the query type, skip this handler
                if (!string.IsNullOrWhiteSpace(query.Type) && !string.Equals(handler.Type, query.Type.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                // if query is null or whitespace, or the handler type is equal to the query type, add the handler's items to the list
                // btw: this will return a full list of items from database, although the ListInventoryAsync calls UoW in behide-which support pagination (skip, take), but repository layer currently doesn't support filtering so this is just a trade-off for now, since I don't want to make another feature for the repository layer to support filtering.
                all.AddRange(await handler.ListInventoryAsync(currentUser.Id, cancellationToken));
            }

            // since LINQ return an IEnumerable, so this conversion is just to make sure that the filtered variable is of type IEnumerable<ConfigurationItemResponse> and not List<ConfigurationItemResponse> -- if not, it may cause conversion exceptions
            // actually, LINQ has the method ToList() but anyway,....
            var filtered = all.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(query.Status))
                filtered = filtered.Where(i => string.Equals(i.Status, query.Status.Trim(), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = query.Search.Trim();
                filtered = filtered.Where(i =>
                    i.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                    || (i.Source?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
                    || (i.ParentName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            var ordered = filtered
                .OrderByDescending(i => i.UpdatedAt)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(i => i.Id)
                .ToList();

            IReadOnlyList<ConfigurationItemResponse> page = ordered
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            var pageInformation = new PageInformation
            {
                Page = query.Page,
                PageSize = query.PageSize,
                TotalItems = ordered.Count,
                ItemsCount = page.Count,
                TotalPages = (int)Math.Ceiling((decimal)ordered.Count / query.PageSize)
            };

            return Result<PagedResponse<IReadOnlyList<ConfigurationItemResponse>>>.Success(
                new PagedResponse<IReadOnlyList<ConfigurationItemResponse>>(page, pageInformation));
        }
    }
}
