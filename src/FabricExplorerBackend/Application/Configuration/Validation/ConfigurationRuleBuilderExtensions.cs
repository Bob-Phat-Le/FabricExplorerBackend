using FluentValidation;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    public static class ConfigurationRuleBuilderExtensions
    {
        /// <summary>The value must be a non-empty GUID.</summary>
        public static IRuleBuilderOptions<T, string?> MustBeGuid<T>(
            this IRuleBuilder<T, string?> ruleBuilder, string errorCode, string message)
            => ruleBuilder
                .Must(value => ConfigurationGuid.TryParse(value, out _))
                .WithErrorCode(errorCode)
                .WithMessage(message);
    }
}
