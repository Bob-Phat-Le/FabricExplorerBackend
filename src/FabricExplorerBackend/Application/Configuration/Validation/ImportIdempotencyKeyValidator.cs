using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Commons.Errors;
using FabricExplorerBackend.Domain.Entities;
using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    public sealed class ImportIdempotencyKeyValidator : AbstractValidator<ImportIdempotencyKey>
    {
        public ImportIdempotencyKeyValidator()
        {
            RuleFor(x => x.Value)
                .MaximumLength(ConfigurationOperation.IdempotencyKeyMaxLength)
                    .WithErrorCode(ErrorCodes.Configuration.InvalidIdempotencyKey)
                    .WithMessage($"Idempotency-Key must be at most {ConfigurationOperation.IdempotencyKeyMaxLength} characters.");
        }
    }
}
