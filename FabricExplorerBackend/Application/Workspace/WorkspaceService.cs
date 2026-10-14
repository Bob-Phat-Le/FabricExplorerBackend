using FabricExplorerBackend.Application.Workspace.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Infrastructures.Cache;
using FabricExplorerBackend.Infrastructures.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Application.Workspace
{
    public class WorkspaceService(
        IUnitOfWork unitOfWork,
        IFabricContextFactory fabricContextFactory,
        ICachedFetcher cachedFetcher,
        IConfiguration configuration) : IWorkspaceService
    {
        public async Task<Result<IEnumerable<WorkspaceListItemResponse>>> GetAllWorkspacesAsync(
            Guid connectionId, bool bypassCache = false, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
            if (connection == null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not fount");

            // Danh sách workspace thay đổi chậm và là đầu vào của mọi dropdown nên cache lâu hơn danh sách item
            var ttl = TimeSpan.FromSeconds(configuration.GetValue("Fabric:WorkspaceListCacheSeconds", 300));
            var key = cachedFetcher.CreateKey("workspaces", connectionId.ToString());

            var workspaces = await cachedFetcher.GetOrFetchAsync<IReadOnlyList<WorkspaceListItemResponse>>(
                key, ttl,
                async ct =>
                {
                    // Quy tắc của fetch dùng chung: chỉ dùng dữ liệu thuần + context, không dùng UnitOfWork
                    var context = await fabricContextFactory.CreateFabricContextWithoutTokenAsync(connection, ct)
                        ?? throw new InvalidOperationException("can not create context");

                    // Fabric chỉ trả các workspace mà service principal của connection là thành viên
                    var list = await context.Client.Core.Workspaces.ListWorkspacesAsync().ToListAsync(ct).AsTask();

                    return list
                        .Select(w => new WorkspaceListItemResponse
                        {
                            WorkspaceId = (Guid)w.Id!,
                            WorkspaceName = w.DisplayName,
                            CapacityId = w.CapacityId
                        })
                        .OrderBy(w => w.WorkspaceName, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                },
                bypassCache,
                cancellationToken: cancellationToken);

            return Result<IEnumerable<WorkspaceListItemResponse>>.Success(workspaces);
        }
    }
}
