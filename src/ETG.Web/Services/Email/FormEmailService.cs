
using CMS.ContactManagement;
using CMS.ContentEngine.Internal;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailLibrary;
using CMS.EmailLibrary.Internal;
using CMS.EmailMarketing;
using CMS.EmailMarketing.Internal;
using Kentico.Xperience.Admin.DigitalMarketing.Internal;

namespace ETG.Web.Services.Email;

public sealed class FormEmailService : IFormEmailService
{
    private readonly IInfoProvider<EmailConfigurationInfo> _emailConfigurationProvider;
    private readonly IInfoProvider<ContentItemInfo> _contentItemProvider;
    private readonly IInfoProvider<EmailChannelInfo> _emailChannelProvider;
    private readonly IInfoProvider<EmailChannelSenderInfo> _emailChannelSenderProvider;
    private readonly IInfoProvider<ContactInfo> _contactProvider;

    private readonly IEmailContentResolverFactory _emailContentResolverFactory;
    private readonly IEmailService _emailService;
    private readonly IEmailMarkupBuilderFactory _emailMarkupBuilderFactory;
    private readonly IEmailChannelLanguageRetriever _languageRetriever;
    private readonly IContentItemDataInfoRetriever _dataRetriever;
    private readonly IEmailChannelSenderEmailProvider _senderEmailProvider;

    private readonly IFormAutoresponderConfigurationService
        _formAutoresponderConfigurationService;

    private readonly ILogger<FormEmailService> _logger;


    public FormEmailService(
        IInfoProvider<EmailConfigurationInfo> emailConfigurationProvider,
        IInfoProvider<ContentItemInfo> contentItemProvider,
        IInfoProvider<EmailChannelInfo> emailChannelProvider,
        IInfoProvider<EmailChannelSenderInfo> emailChannelSenderProvider,
        IInfoProvider<ContactInfo> contactProvider,
        IEmailContentResolverFactory emailContentResolverFactory,
        IEmailService emailService,
        IEmailMarkupBuilderFactory emailMarkupBuilderFactory,
        IEmailChannelLanguageRetriever languageRetriever,
        IContentItemDataInfoRetriever dataRetriever,
        IEmailChannelSenderEmailProvider senderEmailProvider,
        IFormAutoresponderConfigurationService formAutoresponderConfigurationService,
        ILogger<FormEmailService> logger)
    {
        _emailConfigurationProvider = emailConfigurationProvider;
        _contentItemProvider = contentItemProvider;
        _emailChannelProvider = emailChannelProvider;
        _emailChannelSenderProvider = emailChannelSenderProvider;
        _contactProvider = contactProvider;

        _emailContentResolverFactory = emailContentResolverFactory;
        _emailService = emailService;
        _emailMarkupBuilderFactory = emailMarkupBuilderFactory;
        _languageRetriever = languageRetriever;
        _dataRetriever = dataRetriever;
        _senderEmailProvider = senderEmailProvider;

        _formAutoresponderConfigurationService =
            formAutoresponderConfigurationService;

        _logger = logger;
    }


    // ============================================================
    // CUSTOMER CONFIRMATION
    // ============================================================

    public async Task SendCustomerConfirmationAsync(
        string formName,
        string recipientEmail,
        IReadOnlyDictionary<string, string?> formData)
    {
        if (string.IsNullOrWhiteSpace(recipientEmail))
        {
            _logger.LogWarning(
                "Customer confirmation email was not sent because recipient email is empty. Form: {FormName}",
                formName);

            return;
        }


        // --------------------------------------------------------
        // 1. Read Form Autoresponder configuration from Xperience
        // --------------------------------------------------------

        var autoresponder =
            await _formAutoresponderConfigurationService
                .GetConfigurationAsync(formName);


        // --------------------------------------------------------
        // CASE 1:
        // Autoresponder is not configured / disabled.
        // --------------------------------------------------------

        if (autoresponder == null ||
            !autoresponder.IsEnabled)
        {
            _logger.LogInformation(
                "Customer confirmation email skipped for form '{FormName}'. " +
                "No active autoresponder email is configured.",
                formName);

            return;
        }


        // --------------------------------------------------------
        // CASE 2:
        // Autoresponder exists but no email was selected.
        // --------------------------------------------------------

        if (!autoresponder.EmailConfigurationGuid.HasValue)
        {
            _logger.LogInformation(
                "Customer confirmation email skipped for form '{FormName}'. " +
                "Autoresponder is enabled but no Email Channel email is selected.",
                formName);

            return;
        }


        // --------------------------------------------------------
        // 2. Get the selected Email Channel email
        // --------------------------------------------------------

        var emailConfiguration =
            await _emailConfigurationProvider
                .Get()
                .WhereEquals(
                    nameof(EmailConfigurationInfo.EmailConfigurationGUID),
                    autoresponder.EmailConfigurationGuid.Value)
                .FirstOrDefaultAsync();


        // --------------------------------------------------------
        // CASE 3:
        // Selected email no longer exists.
        // --------------------------------------------------------

        if (emailConfiguration == null)
        {
            _logger.LogWarning(
                "Customer confirmation email skipped for form '{FormName}'. " +
                "Selected autoresponder email '{EmailGuid}' could not be found.",
                formName,
                autoresponder.EmailConfigurationGuid.Value);

            return;
        }


        // --------------------------------------------------------
        // 3. Get customer information
        // --------------------------------------------------------

        var firstName = GetValue(formData, "Firstname");
        var lastName = GetValue(formData, "Lastname");


        // --------------------------------------------------------
        // 4. Send using the selected Email Channel configuration
        // --------------------------------------------------------

        await SendEmailAsync(
            formName,
            recipientEmail,
            firstName,
            lastName,
            formData,
            emailConfiguration);
    }


    // ============================================================
    // ADMIN NOTIFICATION
    // ============================================================

    //public async Task SendAdminNotificationAsync(
    //    string formName,
    //    string recipientEmail,
    //    IReadOnlyDictionary<string, string?> formData)
    //{
    //    if (string.IsNullOrWhiteSpace(recipientEmail))
    //    {
    //        _logger.LogWarning(
    //            "Admin notification was not sent because recipient email is empty. Form: {FormName}",
    //            formName);

    //        return;
    //    }




    //    var emailCodeName =
    //        FormEmailConfigurations.GetConfigurationCodeName(formName);


    //    var emailConfiguration =
    //        await _emailConfigurationProvider
    //            .GetAsync(emailCodeName);


    //    if (emailConfiguration == null)
    //    {
    //        _logger.LogWarning(
    //            "Admin notification email configuration '{EmailCodeName}' " +
    //            "was not found for form '{FormName}'.",
    //            emailCodeName,
    //            formName);

    //        return;
    //    }


    //    var firstName = GetValue(formData, "Firstname");
    //    var lastName = GetValue(formData, "Lastname");


    //    await SendEmailAsync(
    //        formName,
    //        recipientEmail,
    //        firstName,
    //        lastName,
    //        formData,
    //        emailConfiguration);
    //}


    // ============================================================
    // COMMON EMAIL SENDING
    // ============================================================

    private async Task SendEmailAsync(
        string formName,
        string recipientEmail,
        string? firstName,
        string? lastName,
        IReadOnlyDictionary<string, string?> formData,
        EmailConfigurationInfo emailConfiguration)
    {
        try
        {
            // ----------------------------------------------------
            // 1. Get or create Contact
            // ----------------------------------------------------

            var contact =
                ContactManagementContext.GetCurrentContact(true)
                ??
                (
                    await _contactProvider
                        .Get()
                        .WhereEquals(
                            nameof(ContactInfo.ContactEmail),
                            recipientEmail)
                        .GetEnumerableTypedResultAsync()
                ).FirstOrDefault();


            if (contact == null)
            {
                contact = new ContactInfo
                {
                    ContactEmail = recipientEmail,
                    ContactFirstName = firstName ?? string.Empty,
                    ContactLastName = lastName ?? string.Empty
                };
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(firstName))
                {
                    contact.ContactFirstName = firstName;
                }

                if (!string.IsNullOrWhiteSpace(lastName))
                {
                    contact.ContactLastName = lastName;
                }

                if (string.IsNullOrWhiteSpace(contact.ContactEmail))
                {
                    contact.ContactEmail = recipientEmail;
                }
            }


            // ----------------------------------------------------
            // 2. Create Xperience email data context
            // ----------------------------------------------------

            var dataContext = new FormEmailDataContext
            {
                Recipient = new Recipient
                {
                    Email = recipientEmail,
                    FirstName = firstName ?? string.Empty,
                    LastName = lastName ?? string.Empty
                }
            };


            foreach (var item in formData)
            {
                if (item.Value != null)
                {
                    dataContext.Items[item.Key] = item.Value;
                }
            }


            // ----------------------------------------------------
            // 3. Build email using Xperience template
            // ----------------------------------------------------

            var markupBuilder =
                await _emailMarkupBuilderFactory
                    .Create(emailConfiguration);


            var mergedTemplate =
                await markupBuilder.BuildEmailForSending(
                    emailConfiguration,
                    SetEmailContext(
                        dataContext.MailoutGuid,
                        contact,
                        recipientEmail,
                        emailConfiguration));


            // ----------------------------------------------------
            // 4. Resolve email content
            // ----------------------------------------------------

            var contentResolver =
                await _emailContentResolverFactory
                    .Create(
                        emailConfiguration,
                        default);


            var emailBody =
                await contentResolver.Resolve(
                    emailConfiguration,
                    mergedTemplate,
                    EmailContentFilterType.Sending,
                    dataContext);


            // ----------------------------------------------------
            // 5. Get email content fields
            // ----------------------------------------------------

            var contentItem =
                await _contentItemProvider.GetAsync(
                    emailConfiguration.EmailConfigurationContentItemID);


            if (contentItem == null)
            {
                throw new InvalidOperationException(
                    $"Email content item was not found for " +
                    $"configuration '{emailConfiguration.EmailConfigurationGUID}'.");
            }


            var contentLanguage =
                await _languageRetriever
                    .GetEmailChannelLanguageInfoOrThrow(
                        emailConfiguration.EmailConfigurationEmailChannelID);


            var data =
                await _dataRetriever.GetContentItemData(
                    contentItem,
                    contentLanguage.ContentLanguageID,
                    false);


            var emailFieldValues =
                new EmailContentTypeSpecificFieldValues(data);


            // ----------------------------------------------------
            // 6. Get email channel
            // ----------------------------------------------------

            var emailChannel =
                (
                    await _emailChannelProvider
                        .Get()
                        .WhereEquals(
                            nameof(EmailChannelInfo.EmailChannelID),
                            emailConfiguration.EmailConfigurationEmailChannelID)
                        .GetEnumerableTypedResultAsync()
                ).FirstOrDefault();


            if (emailChannel == null)
            {
                throw new InvalidOperationException(
                    $"Email channel was not found for configuration " +
                    $"'{emailConfiguration.EmailConfigurationGUID}'.");
            }


            // ----------------------------------------------------
            // 7. Get sender
            // ----------------------------------------------------

            var sender =
                await _emailChannelSenderProvider
                    .GetAsync(
                        emailFieldValues.EmailSenderID);


            if (sender == null)
            {
                throw new InvalidOperationException(
                    $"Email sender was not found for configuration " +
                    $"'{emailConfiguration.EmailConfigurationGUID}'.");
            }


            var senderEmail =
                await _senderEmailProvider.GetEmailAddress(
                    emailChannel.EmailChannelID,
                    sender.EmailChannelSenderName);


            // ----------------------------------------------------
            // 8. Create EmailMessage
            // ----------------------------------------------------

            var emailMessage = new EmailMessage
            {
                From =
                    $"\"{sender.EmailChannelSenderDisplayName}\" <{senderEmail}>",

                Recipients = recipientEmail,

                Subject = emailFieldValues.EmailSubject,

                Body = emailBody,

                PlainTextBody = emailFieldValues.EmailPlainText,

                EmailConfigurationID =
                    emailConfiguration.EmailConfigurationID,

                MailoutGuid =
                    dataContext.MailoutGuid
            };


            // ----------------------------------------------------
            // 9. Add email to Xperience email queue
            // ----------------------------------------------------

            await _emailService.SendEmail(emailMessage);


            _logger.LogInformation(
                "Xperience email queued successfully. " +
                "Form: {FormName}, EmailConfigurationId: {EmailConfigurationId}, Recipient: {Recipient}",
                formName,
                emailConfiguration.EmailConfigurationID,
                recipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send Xperience email. " +
                "Form: {FormName}, EmailConfigurationId: {EmailConfigurationId}, Recipient: {Recipient}",
                formName,
                emailConfiguration.EmailConfigurationID,
                recipientEmail);

            throw;
        }
    }


    // ============================================================
    // FORM VALUE HELPER
    // ============================================================

    private static string? GetValue(
        IReadOnlyDictionary<string, string?> formData,
        string key)
    {
        return formData.TryGetValue(key, out var value)
            ? value
            : null;
    }


    // ============================================================
    // EMAIL CONTEXT
    // ============================================================

    private static Func<IServiceProvider, Task> SetEmailContext(
        Guid mailoutGuid,
        ContactInfo contact,
        string contactEmail,
        EmailConfigurationInfo emailConfiguration)
    {
        return async serviceProvider =>
        {
            var emailRecipientContextProvider =
                serviceProvider
                    .GetRequiredService<IEmailRecipientContextProvider>();

            var recipientContextAccessor =
                serviceProvider
                    .GetRequiredService<IEmailRecipientContextAccessor>();

            var recipientContactGroupAccessor =
                serviceProvider
                    .GetRequiredService<IEmailRecipientContactGroupContextAccessor>();


            var emailRecipientContext =
                await emailRecipientContextProvider.Get(
                    contact.ContactID,
                    contactEmail,
                    emailConfiguration,
                    mailoutGuid,
                    default);


            recipientContextAccessor.SetContext(
                emailRecipientContext);


            if (recipientContactGroupAccessor
                is not EmailRecipientContactGroupContextAccessor accessor)
            {
                return;
            }


            var groupNames =
                contact.ContactGroups
                    .Select(group => group.ContactGroupName)
                    .ToList();


            accessor.Context =
                new EmailRecipientContactGroupContext
                {
                    ContactGroupNames = groupNames
                };
        };
    }
}