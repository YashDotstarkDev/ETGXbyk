using CMS.Automation;
using CMS.DataEngine;
using CMS.OnlineForms;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Microsoft.Extensions.Logging;

[assembly: RegisterAutomationCondition<ETG.Web.Automation.FormFieldValueCondition>(
    identifier: "ETG.FormFieldValueCondition",
    displayName: "Form Field Value Condition",
    Description = "Evaluates a submitted form field value against an expected value.",
    IconName = "xp-form")]

namespace ETG.Web.Automation;

/// <summary>
/// Comparison operators for form field value evaluation.
/// </summary>
public enum ComparisonOperator
{
    Equals = 0,
    Contains = 1,
    NotEquals = 2,
    DoesNotContain = 3
}

/// <summary>
/// Provides dropdown options for comparison operators.
/// </summary>
public class ComparisonOperatorOptionsProvider : IDropDownOptionsProvider
{
    public Task<IEnumerable<DropDownOptionItem>> GetOptionItems() =>
        Task.FromResult<IEnumerable<DropDownOptionItem>>(
        [
            new() { Value = nameof(ComparisonOperator.Equals), Text = "Equals" },
            new() { Value = nameof(ComparisonOperator.Contains), Text = "Contains" },
            new() { Value = nameof(ComparisonOperator.NotEquals), Text = "Not equals" },
            new() { Value = nameof(ComparisonOperator.DoesNotContain), Text = "Does not contain" }
        ]);
}

/// <summary>
/// Configuration properties for the ETG Form Field Value automation condition.
/// </summary>
public class FormFieldValueConditionProperties : IAutomationConditionProperties
{
    [TextInputComponent(
        Label = "Form field name",
        Order = 1,
        ExplanationText = "Enter the code name of the form field to evaluate (e.g., PreferredDestination)")]
    public string FieldName { get; set; } = string.Empty;

    [DropDownComponent(
        Label = "Operator",
        Order = 2,
        DataProviderType = typeof(ComparisonOperatorOptionsProvider))]
    public string Operator { get; set; } = nameof(ComparisonOperator.Equals);

    [TextInputComponent(
        Label = "Expected value",
        Order = 3,
        ExplanationText = "The value to compare the form field against")]
    public string ExpectedValue { get; set; } = string.Empty;
}

/// <summary>
/// Custom automation condition that evaluates a form field value from a form submission.
/// </summary>
public class FormFieldValueCondition : AutomationCondition<FormFieldValueConditionProperties>
{
    private readonly IInfoProvider<BizFormInfo> _bizFormInfoProvider;
    private readonly ILogger<FormFieldValueCondition> _logger;

    public FormFieldValueCondition(
        IInfoProvider<BizFormInfo> bizFormInfoProvider,
        ILogger<FormFieldValueCondition> logger)
    {
        _bizFormInfoProvider = bizFormInfoProvider;
        _logger = logger;
    }

    public override async Task<bool> Evaluate(
        FormFieldValueConditionProperties properties,
        AutomationProcessContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var triggerData = await context.GetTriggerData<FormSubmissionTriggerData>(cancellationToken);
            if (triggerData == null)
            {
                _logger.LogWarning("Form submission trigger data not found.");
                return false;
            }

            var form = await _bizFormInfoProvider
                .Get()
                .WhereEquals(nameof(BizFormInfo.FormID), triggerData.BizFormId)
                .TopN(1)
                .FirstOrDefaultAsync(cancellationToken);

            if (form == null)
            {
                _logger.LogWarning("Form with ID {FormId} not found.", triggerData.BizFormId);
                return false;
            }

            var dataClassInfo = DataClassInfoProvider.GetDataClassInfo(form.FormClassID);
            if (dataClassInfo == null)
            {
                _logger.LogWarning("Form class with ID {ClassId} not found.", form.FormClassID);
                return false;
            }

            var formItem = BizFormItemProvider.GetItem(triggerData.BizFormItemId, dataClassInfo.ClassName);
            if (formItem == null)
            {
                _logger.LogWarning("Form submission with ID {ItemId} not found.", triggerData.BizFormItemId);
                return false;
            }

            var fieldValue = formItem.GetStringValue(properties.FieldName, string.Empty);
            var result = EvaluateCondition(fieldValue, properties.ExpectedValue, properties.Operator);

            _logger.LogInformation(
                "Condition evaluated: Field='{Field}', Value='{Value}', Operator='{Op}', Expected='{Expected}', Result={Result}",
                properties.FieldName, fieldValue, properties.Operator, properties.ExpectedValue, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating form field condition for field '{Field}'", properties.FieldName);
            return false;
        }
    }

    private static bool EvaluateCondition(string actualValue, string expectedValue, string operatorName)
    {
        actualValue = actualValue?.Trim() ?? string.Empty;
        expectedValue = expectedValue?.Trim() ?? string.Empty;

        if (!Enum.TryParse<ComparisonOperator>(operatorName, out var op))
        {
            op = ComparisonOperator.Equals;
        }

        return op switch
        {
            ComparisonOperator.Equals => string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.Contains => actualValue.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.NotEquals => !string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase),
            ComparisonOperator.DoesNotContain => !actualValue.Contains(expectedValue, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
