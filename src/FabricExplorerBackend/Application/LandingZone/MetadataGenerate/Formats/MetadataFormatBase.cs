using FabricExplorerBackend.Application.LandingZone.Contracts.MetadataGenerate;
using System.Text.RegularExpressions;

namespace FabricExplorerBackend.Application.LandingZone.MetadataGenerate.Formats
{
    //[SELF NOTE] This is a base class for metadata formats. It is not intended to be used directly, but rather to be inherited by specific format implementations.
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

        //[SELF NOTE] This property indicates whether the format requires column types to be specified. For CSV and DelimitedText this is true, for JSON and Parquet this is false.
        public virtual bool RequiresColumnTypes => false;
        //[SELF NOTE] This property returns a list of options that can be configured for the format. Each option has a key, a kind (flag, text, choice), a default value, and other metadata. The base implementation returns an empty list, but derived classes can override this to provide specific options.
        //[SELF NOTE] For example, CSV might have options for column separator, row separator, quote character, etc. DelimitedText might have similar options. JSON and Parquet might not have any options.
        public virtual IReadOnlyList<FormatOption> Options => [];

        public virtual void Validate(PreviewRequest request, ICollection<string> errors)
        {
            //[SELF NOTE] This method validates the options provided in the request against the expected options for the format. It checks if required options are present, if values are valid according to choices or patterns, and adds any errors to the errors collection.
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

        //[SELF NOTE] This method retrieves the value of a given option from the request. If the option is not present in the request, it returns the default value defined in the option. If the option is present but has a null value, it returns an empty string.
        //[SELF NOTE] For example, if the option is "ColumnSeparator" and the request has Options["ColumnSeparator"] = ",", it will return ",". If the request does not have that option, it will return the default value defined in the FormatOption object.
        private static string ValueOf(PreviewRequest request, FormatOption option) =>
            request.Options is not null && request.Options.TryGetValue(option.Key, out var value) ? value ?? string.Empty : option.DefaultValue;
    }
}
