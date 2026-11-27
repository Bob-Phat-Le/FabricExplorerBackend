using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Domain.Entities;
using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>
    /// Rules of a saved query entry. Rules run in the order they are declared. The rule on
    /// <c>connectionKey</c> only checks the shape: whether it points anywhere is decided by the
    /// handler, against the file and the system.
    /// </summary>
    public sealed class SavedQueryEntryValidator : AbstractValidator<SavedQueryEntry>
    {
        public SavedQueryEntryValidator()
        {
            RuleFor(x => x.Key)
                .MustBeGuid(ValidationCodes.InvalidKey, "Key must be a non-empty GUID.");

            RuleFor(x => x.Name)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithErrorCode(ValidationCodes.Required)
                    .WithMessage("Name is required.")
                .Must(name => name!.Trim().Length <= SavedQuery.NameMaxLength)
                    .WithErrorCode(ValidationCodes.TooLong)
                    .WithMessage($"Name must be at most {SavedQuery.NameMaxLength} characters.");

            RuleFor(x => x.Description)
                .MaximumLength(SavedQuery.DescriptionMaxLength)
                    .WithErrorCode(ValidationCodes.TooLong)
                    .WithMessage($"Description must be at most {SavedQuery.DescriptionMaxLength} characters.");

            RuleFor(x => x.Sql)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithErrorCode(ValidationCodes.Required)
                    .WithMessage("SQL must not be empty.")
                .MaximumLength(SavedQuery.SqlMaxLength)
                    .WithErrorCode(ValidationCodes.TooLong)
                    .WithMessage($"SQL must be at most {SavedQuery.SqlMaxLength} characters.");

            RuleFor(x => x.Target)
                .NotNull()
                    .WithErrorCode(ValidationCodes.Required)
                    .WithMessage("Target item is required.");

            RuleFor(x => x.Target!)
                .SetValidator(new QueryTargetEntryValidator())
                .When(x => x.Target is not null);

            RuleFor(x => x.Language)
                .Must(language => string.Equals(language!.Trim(), FabricQueryLanguages.TSql, StringComparison.OrdinalIgnoreCase))
                    .WithErrorCode(ValidationCodes.InvalidLanguage)
                    .WithMessage($"Only '{FabricQueryLanguages.TSql}' is supported.")
                    .When(x => !string.IsNullOrWhiteSpace(x.Language));

            RuleFor(x => x.MaxRows)
                .Must(maxRows => maxRows is null or > 0)
                    .WithErrorCode(ValidationCodes.InvalidMaxRows)
                    .WithMessage("maxRows must be greater than zero, or null for unlimited.");

            RuleFor(x => x.ConnectionKey)
                .MustBeGuid(ValidationCodes.ReferenceUnresolved, "connectionKey must be the key of a connection.");
        }
    }
}
