using FabricExplorerBackend.Application.Configuration.Contracts;
using FabricExplorerBackend.Application.Configuration.Handlers;
using FabricExplorerBackend.Application.Configuration.Import;
using FabricExplorerBackend.Commons.Errors;
using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    /// <summary>
    /// Is the import request well formed (mode and selection)? Stops at the first problem.
    /// The document itself is checked by the document reader.
    /// </summary>
    public sealed class ImportRequestValidator : AbstractValidator<ImportRequest>
    {
        public ImportRequestValidator(IConfigurationHandlerRegistry registry, IEnumerable<IImportModeStrategy> modes)
        {
            var modeNames = modes.Select(mode => mode.Name).ToList();

            ClassLevelCascadeMode = CascadeMode.Stop;

            // No mode means the default one.
            RuleFor(x => x.Mode)
                .Must(mode => string.IsNullOrWhiteSpace(mode) || modeNames.Contains(mode.Trim(), StringComparer.OrdinalIgnoreCase))
                    .WithErrorCode(ErrorCodes.Configuration.InvalidMode)
                    .WithMessage($"Mode must be one of: {string.Join(", ", modeNames)}.");

            // No selection means every entry of the file.
            RuleForEach(x => x.Selection!)
                .Must(item => item is not null && registry.TryGet(item.Type, out _) && ConfigurationGuid.TryParse(item.Key, out _))
                    .WithErrorCode(ErrorCodes.Configuration.InvalidSelection)
                    .WithMessage("An entry of the selection has an unknown type or an invalid key.");
        }
    }
}
