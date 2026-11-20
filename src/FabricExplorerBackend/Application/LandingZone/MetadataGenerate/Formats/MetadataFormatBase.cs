using System.Text.RegularExpressions;
using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Typing;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Formats
{
    public abstract class MetadataFormatBase : IMetadataFormat
    {
        private static readonly IReadOnlyDictionary<string, string> Escapes = new Dictionary<string, string>
        {
            ["t"] = "\t",
            ["r"] = "\r",
            ["n"] = "\n",
            ["\\"] = "\\"
        };

        public abstract string Name { get; }
        public abstract string Title { get; }
        public abstract string Description { get; }
        public virtual bool RequiresColumnTypes => false;
        public virtual IReadOnlyList<FormatOption> Options => [];

        public virtual void Validate(PreviewRequest request, ICollection<string> errors)
        {
            foreach (var option in Options)
            {
                var value = ValueOf(request, option);

                if (value.Length == 0)
                {
                    if (option.Required) errors.Add($"{option.Key} is required.");
                }
                else if (option.Choices is not null && !option.Choices.Contains(value))
                {
                    errors.Add($"{option.Key} must be one of: {string.Join(", ", option.Choices)}.");
                }
                else if (option.Pattern is not null && !Regex.IsMatch(value, option.Pattern))
                {
                    errors.Add($"{option.Key} is invalid. {option.Hint}".TrimEnd());
                }
            }
        }

        public virtual IEnumerable<KeyValuePair<string, object?>> Build(PreviewRequest request)
        {
            yield return new("FileFormat", Name);

            foreach (var option in Options)
            {
                var value = ValueOf(request, option);
                if (value.Length == 0) continue;

                yield return new(option.Key, option.Kind switch
                {
                    FormatOptionKind.Flag => bool.Parse(value),
                    FormatOptionKind.EscapedText => Regex.Replace(value, @"\\([trn\\])", m => Escapes[m.Groups[1].Value]),
                    _ => value
                });
            }
        }

        private static string ValueOf(PreviewRequest request, FormatOption option) =>
            request.Options is not null && request.Options.TryGetValue(option.Key, out var value) ? value ?? string.Empty : option.DefaultValue;
    }
}
