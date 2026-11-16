using CMS.ContactManagement;
using CMS.ContentEngine;
using CMS.ContentEngine.Internal;
using CMS.Core;
using CMS.DataEngine;
using CMS.EmailEngine;
using CMS.EmailEngine.Internal;
using CMS.EmailLibrary;
using CMS.EmailLibrary.Internal;
using CMS.EmailMarketing;
using CMS.EmailMarketing.Internal;
using CMS.Helpers;
using CMS.Membership;
using CMS.OnlineForms.Types;
using ETG.Core.Content.Forms;
using Kentico.Xperience.Admin.DigitalMarketing.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ETG.Admin.Features.EnquiryManagement.Enquires;

/// <summary>
/// Sends the existing Xperience Email Builder email to the newly assigned consultant.
/// Each failure stage is logged separately so the Event Log identifies the failing step.
/// </summary>
public sealed class EnquiryAssignmentEmailService
{
    private const string AssignmentEmailCodeName = "EnquireSubmission-9bq38q4a";

    private readonly IEmailService emailService;
    private readonly IUserInfoProvider users;
    private readonly IEventLogService eventLog;

    public EnquiryAssignmentEmailService(
        IEmailService emailService,
        IUserInfoProvider users,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        IEventLogService eventLog)
    {
        this.emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        this.users = users ?? throw new ArgumentNullException(nameof(users));
        this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
    }

    /// <returns>True when Kentico's email service accepts the message; false when a stage fails.</returns>
    public async Task<bool> SendAssignmentEmailAsync(EnquireItem enquiry, Guid assignedUserGuid)
    {
        if (enquiry is null || assignedUserGuid == Guid.Empty)
        {
            return false;
        }

        string stage = "RECIPIENT_VALIDATION";
        try
        {
            var consultant = users.Get()
                .WhereEquals(nameof(UserInfo.UserGUID), assignedUserGuid)
                .FirstOrDefault();

            if (consultant is null)
            {
                throw new InvalidOperationException(
                    $"Assigned consultant '{assignedUserGuid}' was not found. Enquiry ID: {enquiry.ItemID}.");
            }

            string recipientEmail = ValidationHelper.GetString(
                consultant.GetValue(nameof(UserInfo.Email)), string.Empty).Trim();

            if (!ValidationHelper.IsEmail(recipientEmail))
            {
                throw new InvalidOperationException(
                    $"Assigned consultant '{consultant.UserName}' has an invalid or empty email address. Enquiry ID: {enquiry.ItemID}.");
            }

            // A. EMAIL CONFIGURATION LOOKUP
            stage = "EMAIL_CONFIGURATION_LOOKUP";
            var emailConfigurationProvider = Service.Resolve<IInfoProvider<EmailConfigurationInfo>>();
            if (emailConfigurationProvider is null)
            {
                throw new InvalidOperationException("IInfoProvider<EmailConfigurationInfo> is not registered.");
            }

            var emailConfiguration = await emailConfigurationProvider.GetAsync(AssignmentEmailCodeName);
            if (emailConfiguration is null)
            {
                throw new InvalidOperationException(
                    $"Email Builder configuration '{AssignmentEmailCodeName}' was not found. Verify the exact Code name and that the email configuration is available in this environment.");
            }

            if (emailConfiguration.EmailConfigurationContentItemID <= 0)
            {
                throw new InvalidOperationException(
                    $"Email configuration '{AssignmentEmailCodeName}' has no valid content item ID.");
            }

            if (emailConfiguration.EmailConfigurationEmailChannelID <= 0)
            {
                throw new InvalidOperationException(
                    $"Email configuration '{AssignmentEmailCodeName}' has no valid email channel ID.");
            }

            // B. EMAIL TEMPLATE RENDERING
            stage = "EMAIL_TEMPLATE_RENDERING";
            var contentItems = Service.Resolve<IInfoProvider<ContentItemInfo>>();
            var emailChannels = Service.Resolve<IInfoProvider<EmailChannelInfo>>();
            var emailChannelSenders = Service.Resolve<IInfoProvider<EmailChannelSenderInfo>>();
            var contactProvider = Service.Resolve<IInfoProvider<ContactInfo>>();
            var emailContentResolverFactory = Service.Resolve<IEmailContentResolverFactory>();
            var markupBuilderFactory = Service.Resolve<IEmailMarkupBuilderFactory>();
            var languageRetriever = Service.Resolve<IEmailChannelLanguageRetriever>();
            var contentItemDataRetriever = Service.Resolve<IContentItemDataInfoRetriever>();
            var senderEmailProvider = Service.Resolve<IEmailChannelSenderEmailProvider>();

            var currentContact = (await contactProvider.Get()
                    .WhereEquals(nameof(ContactInfo.ContactEmail), recipientEmail)
                    .GetEnumerableTypedResultAsync())
                .FirstOrDefault()
                ?? new ContactInfo { ContactEmail = recipientEmail };

            var recipient = new Recipient
            {
                Email = recipientEmail,
                FirstName = consultant.FirstName,
                LastName = consultant.LastName
            };

            var dataContext = new FormAutoresponderEmailDataContext
            {
                Recipient = recipient
            };

            var markupBuilder = await markupBuilderFactory.Create(emailConfiguration);
            if (markupBuilder is null)
            {
                throw new InvalidOperationException("Email markup builder could not be created.");
            }

            string mergedTemplate = await markupBuilder.BuildEmailForSending(
                emailConfiguration,
                SetEmailContext(dataContext.MailoutGuid, currentContact, recipientEmail, emailConfiguration));

            if (string.IsNullOrWhiteSpace(mergedTemplate))
            {
                throw new InvalidOperationException(
                    $"Email Builder returned an empty template for '{AssignmentEmailCodeName}'.");
            }

            var contentResolver = await emailContentResolverFactory.Create(emailConfiguration, default);
            if (contentResolver is null)
            {
                throw new InvalidOperationException("Email content resolver could not be created.");
            }

            string emailBody = await contentResolver.Resolve(
                emailConfiguration,
                mergedTemplate,
                EmailContentFilterType.Sending,
                dataContext);

            if (string.IsNullOrWhiteSpace(emailBody))
            {
                throw new InvalidOperationException(
                    $"Email Builder rendered an empty email body for '{AssignmentEmailCodeName}'. Check the email content and its language variant.");
            }

            var contentItem = await contentItems.GetAsync(emailConfiguration.EmailConfigurationContentItemID);
            if (contentItem is null)
            {
                throw new InvalidOperationException(
                    $"Content item ID '{emailConfiguration.EmailConfigurationContentItemID}' for '{AssignmentEmailCodeName}' was not found.");
            }

            var contentLanguage = await languageRetriever.GetEmailChannelLanguageInfoOrThrow(
                emailConfiguration.EmailConfigurationEmailChannelID);
            var contentItemData = await contentItemDataRetriever.GetContentItemData(
                contentItem,
                contentLanguage.ContentLanguageID,
                false);
            if (contentItemData is null)
            {
                throw new InvalidOperationException("Email content item data could not be loaded for the channel language.");
            }

            var emailFieldValues = new EmailContentTypeSpecificFieldValues(contentItemData);
            if (string.IsNullOrWhiteSpace(emailFieldValues.EmailSubject))
            {
                throw new InvalidOperationException(
                    $"Email Builder email '{AssignmentEmailCodeName}' has an empty subject.");
            }

            var emailChannel = (await emailChannels.Get()
                    .WhereEquals(nameof(EmailChannelInfo.EmailChannelID), emailConfiguration.EmailConfigurationEmailChannelID)
                    .GetEnumerableTypedResultAsync())
                .FirstOrDefault();
            if (emailChannel is null)
            {
                throw new InvalidOperationException(
                    $"Email channel ID '{emailConfiguration.EmailConfigurationEmailChannelID}' for '{AssignmentEmailCodeName}' was not found.");
            }

            var sender = await emailChannelSenders.GetAsync(emailFieldValues.EmailSenderID);
            if (sender is null)
            {
                throw new InvalidOperationException(
                    $"Sender ID '{emailFieldValues.EmailSenderID}' selected in '{AssignmentEmailCodeName}' was not found.");
            }

            string senderAddress = await senderEmailProvider.GetEmailAddress(
                emailChannel.EmailChannelID,
                sender.EmailChannelSenderName);
            if (!ValidationHelper.IsEmail(senderAddress))
            {
                throw new InvalidOperationException(
                    $"The sender configured in '{AssignmentEmailCodeName}' has an invalid or empty email address.");
            }

            // C. EMAIL SENDING
            stage = "EMAIL_SENDING";
            using var message = new EmailMessage
            {
                From = $"\"{sender.EmailChannelSenderDisplayName}\" <{senderAddress}>",
                Recipients = recipientEmail,
                Subject = $"{emailFieldValues.EmailSubject} - Enquiry #{enquiry.ItemID}",
                Body = emailBody,
                PlainTextBody = emailFieldValues.EmailPlainText,
                EmailConfigurationID = emailConfiguration.EmailConfigurationID,
                MailoutGuid = dataContext.MailoutGuid
            };

            await emailService.SendEmail(message);
            return true;
        }
        catch (Exception ex)
        {
            // The stage code makes it possible to distinguish lookup, rendering and transport failures.
            eventLog.LogException(
                nameof(EnquiryAssignmentEmailService),
                $"ASSIGNMENT_{stage}_FAILED",
                new InvalidOperationException(
                    $"Assignment email failed at stage '{stage}'. Enquiry ID: {enquiry.ItemID}; assigned user GUID: {assignedUserGuid}.", ex));
            return false;
        }
    }

    private static Func<IServiceProvider, Task> SetEmailContext(
        Guid mailoutGuid,
        ContactInfo currentContact,
        string contactEmail,
        EmailConfigurationInfo emailConfiguration)
    {
        return async serviceProvider =>
        {
            var emailRecipientContextProvider = serviceProvider.GetRequiredService<IEmailRecipientContextProvider>();
            var recipientContextAccessor = serviceProvider.GetRequiredService<IEmailRecipientContextAccessor>();
            var recipientContactGroupAccessor = serviceProvider.GetRequiredService<IEmailRecipientContactGroupContextAccessor>();

            var emailRecipientContext = await emailRecipientContextProvider.Get(
                currentContact.ContactID,
                contactEmail,
                emailConfiguration,
                mailoutGuid,
                default);
            recipientContextAccessor.SetContext(emailRecipientContext);

            if (recipientContactGroupAccessor is EmailRecipientContactGroupContextAccessor accessor)
            {
                accessor.Context = new EmailRecipientContactGroupContext
                {
                    ContactGroupNames = currentContact.ContactGroups
                        .Select(group => group.ContactGroupName)
                        .ToList()
                };
            }
        };
    }
}
