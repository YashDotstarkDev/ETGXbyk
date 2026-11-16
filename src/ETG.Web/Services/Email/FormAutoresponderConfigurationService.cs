using CMS.Automation.Internal;
using CMS.AutomationEngine.Internal;
using CMS.DataEngine;
using CMS.EmailLibrary;
using CMS.OnlineForms;

namespace ETG.Web.Services.Email;

public sealed class FormAutoresponderConfigurationService
    : IFormAutoresponderConfigurationService
{
    private readonly IInfoProvider<BizFormInfo> _bizFormInfoProvider;
    private readonly IInfoProvider<ObjectWorkflowTriggerInfo> _triggerInfoProvider;
    private readonly IInfoProvider<WorkflowInfo> _workflowInfoProvider;
    private readonly IInfoProvider<WorkflowStepInfo> _workflowStepInfoProvider;
    private readonly IInfoProvider<EmailConfigurationInfo> _emailConfigurationProvider;
    private readonly ILogger<FormAutoresponderConfigurationService> _logger;

    public FormAutoresponderConfigurationService(
        IInfoProvider<BizFormInfo> bizFormInfoProvider,
        IInfoProvider<ObjectWorkflowTriggerInfo> triggerInfoProvider,
        IInfoProvider<WorkflowInfo> workflowInfoProvider,
        IInfoProvider<WorkflowStepInfo> workflowStepInfoProvider,
        IInfoProvider<EmailConfigurationInfo> emailConfigurationProvider,
        ILogger<FormAutoresponderConfigurationService> logger)
    {
        _bizFormInfoProvider = bizFormInfoProvider;
        _triggerInfoProvider = triggerInfoProvider;
        _workflowInfoProvider = workflowInfoProvider;
        _workflowStepInfoProvider = workflowStepInfoProvider;
        _emailConfigurationProvider = emailConfigurationProvider;
        _logger = logger;
    }


    public async Task<FormAutoresponderConfiguration?> GetConfigurationAsync(
        string formName)
    {
        if (string.IsNullOrWhiteSpace(formName))
        {
            return null;
        }

        // 1. Get the exact form

        var form = await _bizFormInfoProvider
            .Get()
            .WhereEquals(
                nameof(BizFormInfo.FormName),
                formName)
            .FirstOrDefaultAsync();

        if (form == null)
        {
            _logger.LogWarning(
                "Form '{FormName}' was not found while resolving autoresponder.",
                formName);

            return null;
        }


        _logger.LogInformation(
            "Resolving autoresponder for form '{FormName}' with FormID {FormID}.",
            formName,
            form.FormID);

        // 2. Get ALL triggers for THIS form

        var triggers = await _triggerInfoProvider
            .Get()
            .WhereEquals(
                "TriggerObjectType",
                "cms.form")
            .WhereEquals(
                "TriggerTargetObjectType",
                "cms.form")
            .WhereEquals(
                "TriggerTargetObjectID",
                form.FormID)
            .GetEnumerableTypedResultAsync();

        var triggerList = triggers.ToList();

        if (triggerList.Count == 0)
        {
            _logger.LogInformation(
                "No automation trigger exists for form '{FormName}'. " +
                "Autoresponder is not configured.",
                formName);

            return Disabled();
        }


        _logger.LogInformation(
            "Found {TriggerCount} automation trigger(s) for form '{FormName}'.",
            triggerList.Count,
            formName);

        // 3. Check every workflow attached to THIS form

        foreach (var trigger in triggerList)
        {
            var workflow = await _workflowInfoProvider
                .Get()
                .WhereEquals(
                    "WorkflowID",
                    trigger.TriggerWorkflowID)
                .FirstOrDefaultAsync();

            if (workflow == null)
            {
                _logger.LogWarning(
                    "Workflow {WorkflowID} was not found for form '{FormName}'.",
                    trigger.TriggerWorkflowID,
                    formName);

                continue;
            }

            if (!workflow.WorkflowEnabled)
            {
                _logger.LogInformation(
                    "Workflow {WorkflowID} is disabled for form '{FormName}'.",
                    workflow.WorkflowID,
                    formName);

                continue;
            }

            // 4. Get ALL steps for this exact workflow

            var workflowSteps = await _workflowStepInfoProvider
                .Get()
                .WhereEquals(
                    "StepWorkflowID",
                    workflow.WorkflowID)
                .GetEnumerableTypedResultAsync();

            var steps = workflowSteps.ToList();

            _logger.LogInformation(
                "Workflow {WorkflowID} for form '{FormName}' has {StepCount} step(s).",
                workflow.WorkflowID,
                formName,
                steps.Count);

            // 5. Look through every step for EmailGuid

            foreach (var step in steps)
            {
                var stepActionParameters =
                    step.GetStringValue(
                        "StepActionParameters",
                        null);

                if (string.IsNullOrWhiteSpace(stepActionParameters))
                {
                    continue;
                }


                _logger.LogDebug(
                    "Checking workflow step {StepID} for form '{FormName}'. " +
                    "StepType: {StepType}. Parameters: {Parameters}",
                    step.StepID,
                    formName,
                    step.StepType,
                    stepActionParameters);


                var emailGuid =  ExtractEmailGuid( stepActionParameters, formName, step.StepID);

                if (!emailGuid.HasValue)
                {
                    continue;
                }

                // 6. Make sure this email actually exists

                var emailConfiguration =
                    await _emailConfigurationProvider
                        .Get()
                        .WhereEquals(
                            nameof(EmailConfigurationInfo.EmailConfigurationGUID),
                            emailGuid.Value)
                        .FirstOrDefaultAsync();

                if (emailConfiguration == null)
                {
                    _logger.LogWarning(
                        "Workflow step {StepID} for form '{FormName}' " +
                        "contains EmailGuid '{EmailGuid}', but the email " +
                        "configuration does not exist.",
                        step.StepID,
                        formName,
                        emailGuid.Value);

                    continue;
                }

                // 7. We found the selected autoresponder email

                _logger.LogInformation(
                    "Selected autoresponder email found for form '{FormName}'. " +
                    "EmailGuid: {EmailGuid}, EmailConfigurationID: {EmailConfigurationID}, " +
                    "WorkflowID: {WorkflowID}, StepID: {StepID}",
                    formName,
                    emailGuid.Value,
                    emailConfiguration.EmailConfigurationID,
                    workflow.WorkflowID,
                    step.StepID);


                return new FormAutoresponderConfiguration
                {
                    IsEnabled = true,
                    EmailConfigurationGuid = emailGuid.Value
                };
            }
        }

        // 8. No selected email was found

        _logger.LogInformation(
            "No selected Form autoresponder email was found for form '{FormName}'.",
            formName);

        return Disabled();
    }


    private static FormAutoresponderConfiguration Disabled()
    {
        return new FormAutoresponderConfiguration
        {
            IsEnabled = false,
            EmailConfigurationGuid = null
        };
    }


    private Guid? ExtractEmailGuid(string? parameters, string formName, int stepId)
    {
        if (string.IsNullOrWhiteSpace(parameters))
        {
            return null;
        }

        try
        {
            var document = System.Xml.Linq.XDocument.Parse(parameters);

            var emailGuidValue = document
                .Descendants("EmailGuid")
                .Select(x => x.Value)
                .FirstOrDefault();

            if (Guid.TryParse(emailGuidValue, out var emailGuid))
            {
                return emailGuid;
            }
        }
        catch (System.Xml.XmlException ex)
        {
            _logger.LogWarning(
                ex,
                "Unable to parse StepActionParameters for workflow step {StepID} " +
                "while resolving autoresponder for form '{FormName}'.",
                stepId,
                formName);
        }

        return null;
    }
}