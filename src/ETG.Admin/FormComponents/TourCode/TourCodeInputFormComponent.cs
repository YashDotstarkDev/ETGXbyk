using CMS.ContentEngine;
using CMS.Core;
using CMS.Websites;
using Kentico.Xperience.Admin.Base.Forms;

[assembly: RegisterFormComponent(
    ETG.Admin.FormComponents.TourCode.TourCodeInputFormComponent.IDENTIFIER,
    typeof(ETG.Admin.FormComponents.TourCode.TourCodeInputFormComponent),
    "ETG Tour Code Input")]

namespace ETG.Admin.FormComponents.TourCode;

public sealed class TourCodeInputFormComponent
    : FormComponent<TourCodeInputClientProperties, string>
{
    public const string IDENTIFIER = "ETG.Admin.TourCodeInput";

    private readonly UniqueTourCodeRule uniqueTourCodeRule;

    public TourCodeInputFormComponent(IContentQueryExecutor contentQueryExecutor)
    {
        uniqueTourCodeRule = new UniqueTourCodeRule(contentQueryExecutor);
    }

    public override string ClientComponentName =>
        "@website/web-admin/TourCodeInput";

    protected override void ConfigureComponent()
    {
        base.ConfigureComponent();
        AddValidationRule(uniqueTourCodeRule);
    }
}

public sealed class TourCodeInputClientProperties
    : FormComponentClientProperties<string>
{
}

internal sealed class UniqueTourCodeRuleProperties : ValidationRuleProperties
{
    public override string GetDescriptionText(ILocalizationService localizationService)
        => "Checks that the entered Tour Code is not already used by another page.";
}

internal sealed class UniqueTourCodeRule
    : ValidationRule<UniqueTourCodeRuleProperties, string>
{
    private const string TourCodeFieldName = "TourCode";

    private readonly IContentQueryExecutor contentQueryExecutor;

    public UniqueTourCodeRule(IContentQueryExecutor contentQueryExecutor)
    {
        this.contentQueryExecutor = contentQueryExecutor;
    }

    public override async Task<ValidationResult> Validate(
    string value,
    IFormFieldValueProvider formFieldValueProvider)
    {
        string enteredCode = value?.Trim() ?? string.Empty;

        if (enteredCode.Length == 0)
        {
            return new ValidationResult(false, "Tour code cannot be blank");
        }

        if (FormContext is not IContentItemFormContextBase currentPage)
        {
            throw new InvalidOperationException("Tour Code validation must run on a content item form.");
        }

        try
        {
            var query = new ContentItemQueryBuilder()
                .ForContentTypes(parameters => parameters
                    .ForWebsite(includeUrlPath: false)
                    .WithContentTypeFields());

            var pages = await contentQueryExecutor.GetResult(
                query,
                item => new
                {
                    ItemId = item.ContentItemID,
                    HasTourCode = item.TryGetValue<string>(TourCodeFieldName, out string? storedCode),
                    Code = storedCode
                },
                new ContentQueryExecutionOptions
                {
                    ForPreview = true,
                    IncludeSecuredItems = true
                });

            bool duplicateExists = pages.Any(page =>
                page.ItemId != currentPage.ItemId &&
                page.HasTourCode &&
                string.Equals(page.Code?.Trim(), enteredCode, StringComparison.OrdinalIgnoreCase));

            return duplicateExists
                ? new ValidationResult(false, "Tour code needs to be unique")
                : ValidationResult.Success;
        }
        catch (Exception ex)
        {
            Service.Resolve<IEventLogService>()
                .LogException("TourCodeValidation", "QUERY_FAILED", ex);

            return new ValidationResult(false, "Could not verify Tour code uniqueness right now. Please try saving again.");
        }
    }
}