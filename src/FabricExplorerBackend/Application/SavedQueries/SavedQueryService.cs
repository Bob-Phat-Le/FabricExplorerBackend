using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Validation;
using FabricExplorerBackend.Application.SavedQueries.Contracts;
using FabricExplorerBackend.Commons.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Commons.Paginations;
using FabricExplorerBackend.Commons.Results;
using FabricExplorerBackend.Commons.Security;
using FabricExplorerBackend.Domain.Entities;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FluentValidation;

namespace FabricExplorerBackend.Application.SavedQueries
{
    public class SavedQueryService(
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<SavedQueryEntry> validator,
        ISavedQueryMapper mapper,
        TimeProvider clock) : ISavedQueryService
    {
        public async Task<Result<PagedResponse<IEnumerable<SavedQueryResponse>>>> GetAllAsync(
            ListSavedQueriesRequest request, CancellationToken cancellationToken = default)
        {
            request.Validate();

            var filter = new SavedQueryFilter(request.Search, request.ConnectionId, request.ItemType);
            var (queries, count) = await unitOfWork.SavedQueryRepository.SearchAsync(
                currentUser.Id, filter, (request.Page - 1) * request.PageSize, request.PageSize, cancellationToken);

            var connections = await unitOfWork.ConnectionRepository.GetByIdsAsync(
                queries.Select(q => q.ConnectionId).Distinct().ToList(), cancellationToken: cancellationToken);
            var names = connections.ToDictionary(c => c.Id, c => c.Name);

            var items = queries
                .Select(q => mapper.ToResponse(q, names.GetValueOrDefault(q.ConnectionId)))
                .ToList();

            var pageInformation = new PageInformation
            {
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = count,
                ItemsCount = items.Count,
                TotalPages = (int)Math.Ceiling((decimal)count / request.PageSize)
            };

            return Result<PagedResponse<IEnumerable<SavedQueryResponse>>>.Success(
                new PagedResponse<IEnumerable<SavedQueryResponse>>(items, pageInformation));
        }

        public async Task<Result<SavedQueryResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var query = await unitOfWork.SavedQueryRepository.GetOwnedAsync(currentUser.Id, id, cancellationToken: cancellationToken);
            if (query is null)
                return Error.NotFound(ErrorCodes.SavedQuery.NotFound, "Saved query not found");

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(query.ConnectionId, cancellationToken: cancellationToken);
            return Result<SavedQueryResponse>.Success(mapper.ToResponse(query, connection?.Name));
        }

        public async Task<Result<SavedQueryResponse>> CreateAsync(CreateSavedQueryRequest request, CancellationToken cancellationToken = default)
        {
            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(request.ConnectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            var key = Guid.NewGuid();
            var entry = new SavedQueryEntry
            {
                Key = key.ToString("D"),
                Name = request.Name,
                Description = request.Description,
                ConnectionKey = connection.ExternalKey.ToString("D"),
                Target = new QueryTargetEntry
                {
                    ItemType = request.Target?.ItemType,
                    ItemId = request.Target?.ItemId.ToString("D"),
                    ItemName = request.Target?.ItemName
                },
                Language = request.Language,
                Sql = request.Sql,
                MaxRows = request.MaxRows
            };
            if (request.Target is null)
                entry.Target = null;

            var issues = validator.Validate(entry).ToIssues();
            if (issues.Count > 0)
                return Error.BadRequest(ErrorCodes.SavedQuery.BadRequest, $"{issues[0].Field}: {issues[0].Message}");

            var name = request.Name!.Trim();
            var duplicate = await unitOfWork.SavedQueryRepository.FindByNameAsync(currentUser.Id, connection.Id, name, cancellationToken: cancellationToken);
            if (duplicate is not null)
                return Error.Conflict(ErrorCodes.SavedQuery.Duplicated, "A saved query with this name already exists for the connection");

            var query = new SavedQuery
            {
                Id = Guid.NewGuid(),
                ExternalKey = key,
                OwnerId = currentUser.Id,
                ConnectionId = connection.Id,
                CreatedAt = clock.GetUtcNow()
            };
            mapper.Apply(entry, query);

            await unitOfWork.SavedQueryRepository.AddAsync(query, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<SavedQueryResponse>.Success(mapper.ToResponse(query, connection.Name));
        }

        public async Task<Result<SavedQueryResponse>> UpdateAsync(Guid id, UpdateSavedQueryRequest request, CancellationToken cancellationToken = default)
        {
            var query = await unitOfWork.SavedQueryRepository.GetOwnedAsync(currentUser.Id, id, trackChanges: true, cancellationToken);
            if (query is null)
                return Error.NotFound(ErrorCodes.SavedQuery.NotFound, "Saved query not found");

            var connection = await unitOfWork.ConnectionRepository.GetByIdAsync(query.ConnectionId, cancellationToken: cancellationToken);
            if (connection is null)
                return Error.NotFound(ErrorCodes.Connection.NotFound, "Connection not found");

            // Start from the stored state, apply what was sent, and validate the result as a whole.
            var entry = mapper.ToEntry(query, connection.ExternalKey);
            entry.Name = request.Name ?? entry.Name;
            entry.Description = request.Description ?? entry.Description;
            entry.Sql = request.Sql ?? entry.Sql;
            entry.Language = request.Language ?? entry.Language;
            entry.MaxRows = request.UnlimitedRows ? null : request.MaxRows ?? entry.MaxRows;
            if (request.Target is not null)
                entry.Target = new QueryTargetEntry
                {
                    ItemType = request.Target.ItemType,
                    ItemId = request.Target.ItemId.ToString("D"),
                    ItemName = request.Target.ItemName
                };

            var issues = validator.Validate(entry).ToIssues();
            if (issues.Count > 0)
                return Error.BadRequest(ErrorCodes.SavedQuery.BadRequest, $"{issues[0].Field}: {issues[0].Message}");

            var name = entry.Name!.Trim();
            if (!string.Equals(name, query.Name, StringComparison.Ordinal))
            {
                var duplicate = await unitOfWork.SavedQueryRepository.FindByNameAsync(currentUser.Id, query.ConnectionId, name, cancellationToken: cancellationToken);
                if (duplicate is not null && duplicate.Id != query.Id)
                    return Error.Conflict(ErrorCodes.SavedQuery.Duplicated, "A saved query with this name already exists for the connection");
            }

            mapper.Apply(entry, query);
            query.UpdatedAt = clock.GetUtcNow();

            unitOfWork.SavedQueryRepository.Update(query);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<SavedQueryResponse>.Success(mapper.ToResponse(query, connection.Name));
        }

        public async Task<Result<ConfirmationResponse>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var query = await unitOfWork.SavedQueryRepository.GetOwnedAsync(currentUser.Id, id, trackChanges: true, cancellationToken);
            if (query is null)
                return Error.NotFound(ErrorCodes.SavedQuery.NotFound, "Saved query not found");

            unitOfWork.SavedQueryRepository.Delete(query);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<ConfirmationResponse>.Success(new ConfirmationResponse { Message = "Saved query deleted successfully" });
        }
    }
}
