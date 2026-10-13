using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(
    ETG.Admin.FormComponents.MultiChoice.MultiChoiceSelectorFormComponent.IDENTIFIER,
    typeof(ETG.Admin.FormComponents.MultiChoice.MultiChoiceSelectorFormComponent),
    "Multichoice Selector")]

namespace ETG.Admin.FormComponents.MultiChoice;

public sealed class MultiChoiceSelectorFormComponent
    : FormComponent<
        MultiChoiceSelectorProperties,
        MultiChoiceSelectorClientProperties,
        string>
{
    public const string IDENTIFIER = "ReusableComponents.MultiChoiceSelector";

    public override string ClientComponentName =>
        "@website/web-admin/MultiChoiceSelector";

    protected override Task ConfigureClientProperties(
        MultiChoiceSelectorClientProperties clientProperties)
    {
        var separator = string.IsNullOrEmpty(Properties.DataSourceValueSeparator)
            ? ";"
            : Properties.DataSourceValueSeparator;

        clientProperties.Options = (Properties.Options ?? string.Empty)
            .Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .Select(line =>
            {
                var separatorPosition = line.IndexOf(
                    separator,
                    StringComparison.Ordinal);

                if (separatorPosition < 0)
                {
                    return new MultiChoiceOption
                    {
                        Value = line,
                        Label = line
                    };
                }

                var value = line[..separatorPosition].Trim();
                var label = line[(separatorPosition + separator.Length)..].Trim();

                return new MultiChoiceOption
                {
                    Value = value,
                    Label = string.IsNullOrEmpty(label) ? value : label
                };
            })
            .Where(option => !string.IsNullOrEmpty(option.Value))
            .ToArray();

        return base.ConfigureClientProperties(clientProperties);
    }
}

public sealed class MultiChoiceSelectorProperties : FormComponentProperties
{
    [TextAreaComponent(
        Label = "Data source",
        Order = 1,
        ExplanationText =
            "Specify one item per line in 'value<separator>text' format.")]
    public string Options { get; set; } = string.Empty;

    [TextInputComponent(
        Label = "Data source value separator",
        Order = 2,
        ExplanationText =
            "The string that separates 'value' from 'text' within one option entry.")]
    public string DataSourceValueSeparator { get; set; } = ";";
}

public sealed class MultiChoiceSelectorClientProperties
    : FormComponentClientProperties<string>
{
    public MultiChoiceOption[] Options { get; set; } = [];
}

public sealed class MultiChoiceOption
{
    public string Value { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}