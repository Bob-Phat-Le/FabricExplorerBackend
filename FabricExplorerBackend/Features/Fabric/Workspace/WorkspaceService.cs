using Azure;
using Azure.Identity;
using FabricExplorerBackend.Commons;
using FabricExplorerBackend.Commons.Models.Responses.Workspace;
using FabricExplorerBackend.Domain.Enums;
using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;

namespace FabricExplorerBackend.Features.Fabric.Workspace
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
            try
            {
                // Tra connection trước (rẻ) để connection đã xóa/không tồn tại luôn trả 404, kể cả khi cache còn dữ liệu
                var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(connectionId, cancellationToken: cancellationToken);
                if (connection == null)
                    return Result<IEnumerable<WorkspaceListItemResponse>>.Failure(ResultStatus.NotFound, "connection not found");

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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (AuthenticationFailedException ex)
            {
                return Result<IEnumerable<WorkspaceListItemResponse>>.Failure(ResultStatus.BadRequest, ex.Message);
            }
            catch (RequestFailedException ex) when (ex.Status is 401 or 403)
            {
                return Result<IEnumerable<WorkspaceListItemResponse>>.Failure(
                    ResultStatus.Forbidden, "The connection is not allowed to list workspaces.");
            }
            catch (Exception ex)
            {
                return Result<IEnumerable<WorkspaceListItemResponse>>.Failure(ResultStatus.InternalError, ex.Message);
            }
        }
    }
}
