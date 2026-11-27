using FabricExplorerBackend.Commons.Results;

namespace FabricExplorerBackend.Application.Configuration.Validation
{
    public static class ValidationResultExtensions
    {
        /// <summary>The failures as the issues of a preview / import response; the field is the JSON name of the property.</summary>
        public static IReadOnlyList<ValidationIssue> ToIssues(this FluentValidation.Results.ValidationResult result)
            => result.Errors
                .Select(failure => new ValidationIssue(failure.ErrorCode, ToFieldName(failure.PropertyName), failure.ErrorMessage))
                .ToList();

        /// <summary>The first failure as a bad request. Validators of a request stop at the first failure.</summary>
        public static Error ToBadRequest(this FluentValidation.Results.ValidationResult result)
        {
            var failure = result.Errors[0];
            return Error.BadRequest(failure.ErrorCode, failure.ErrorMessage);
        }

        /// <summary>"Target.ItemType" -> "target.itemType".</summary>
        private static string ToFieldName(string propertyName)
            => string.Join('.', propertyName
                .Split('.')
                .Select(segment => segment.Length == 0 ? segment : char.ToLowerInvariant(segment[0]) + segment[1..]));
    }
}
