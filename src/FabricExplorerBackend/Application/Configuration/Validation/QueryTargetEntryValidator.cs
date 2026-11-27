using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Domain.Entities;
using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>Rules of the item a saved query runs against.</summary>
    public sealed class QueryTargetEntryValidator : AbstractValidator<QueryTargetEntry>
    {
        public QueryTargetEntryValidator()
        {
            RuleFor(x => x.ItemType)
                .Must(itemType => FabricItemTypes.All.Contains(itemType ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    .WithErrorCode(ValidationCodes.InvalidItemType)
                    .WithMessage($"Item type must be one of: {string.Join(", ", FabricItemTypes.All)}.");

            RuleFor(x => x.ItemId)
                .MustBeGuid(ValidationCodes.InvalidGuid, "Item id must be a GUID.");

            RuleFor(x => x.ItemName)
                .MaximumLength(SavedQuery.ItemNameMaxLength)
                    .WithErrorCode(ValidationCodes.TooLong)
                    .WithMessage($"Item name must be at most {SavedQuery.ItemNameMaxLength} characters.");
        }
    }
}
