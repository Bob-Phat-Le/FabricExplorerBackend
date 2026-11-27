using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Documents;
using FabricExplorerBackend.Application.Configuration.Handlers;
using FabricExplorerBackend.Commons.Errors;
using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>Is the export request well formed? Stops at the first problem.</summary>
    public sealed class ExportRequestValidator : AbstractValidator<ExportRequest>
    {
        public ExportRequestValidator(IConfigurationHandlerRegistry registry)
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Scope)
                .Must(scope => ConfigurationFormat.NormalizeScope(scope) is ConfigurationFormat.ScopeAll or ConfigurationFormat.ScopeSelected)
                    .WithErrorCode(ErrorCodes.Configuration.InvalidSelection)
                    .WithMessage("Scope must be 'all' or 'selected'.");

            When(x => ConfigurationFormat.NormalizeScope(x.Scope) == ConfigurationFormat.ScopeSelected, () =>
            {
                RuleFor(x => x.Items)
                    .Must(items => items is { Count: > 0 })
                        .WithErrorCode(ErrorCodes.Configuration.InvalidSelection)
                        .WithMessage("Choose at least one item to export.");

                RuleForEach(x => x.Items!)
                    .Must(item => item is not null && item.Id != Guid.Empty && registry.TryGet(item.Type, out _))
                        .WithErrorCode(ErrorCodes.Configuration.InvalidSelection)
                        .WithMessage("An item of the selection has an unknown type or an empty id.");
            });
        }
    }
}
