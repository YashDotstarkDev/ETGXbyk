using CMS.Core;
using CMS.EmailEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.OnlineForms.Types;
using ETG.Core.Content.Forms;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ETG.Admin.Features.EnquiryManagement.Enquires;

/// <summary>
/// Called by the Enquiry Management PageCommands after the enquiry has been saved.
/// It sends only for a non-empty, changed consultant assignment.
/// </summary>
public static class EnquiryAssignmentEventHandler
{
    public static async Task HandleAssignmentChangedAsync(
        EnquireItem savedEnquiry,
        Guid previousAssigneeGuid,
        IUserInfoProvider users,
        IEmailService emailService,
        IConfiguration configuration,
        IEventLogService eventLog)
    {
        if (savedEnquiry is null)
        {
            return;
        }

        try
        {
            Guid newAssigneeGuid = ValidationHelper.GetGuid(
                savedEnquiry.GetValue("AssignedUserGuid"), Guid.Empty);

            // Do not send if the assignment is cleared or unchanged.
            if (newAssigneeGuid == Guid.Empty || newAssigneeGuid == previousAssigneeGuid)
            {
                return;
            }

            var consultant = users.Get()
                .WhereEquals(nameof(UserInfo.UserGUID), newAssigneeGuid)
                .FirstOrDefault();

            if (consultant is null)
            {
                eventLog.LogException(nameof(EnquiryAssignmentEventHandler), "ASSIGNED_CONSULTANT_NOT_FOUND",
                    new InvalidOperationException($"Consultant '{newAssigneeGuid}' was not found. Enquiry ID: {savedEnquiry.ItemID}."));
                return;
            }

            string email = ValidationHelper.GetString(consultant.GetValue(nameof(UserInfo.Email)), string.Empty).Trim();
            if (!ValidationHelper.IsEmail(email))
            {
                eventLog.LogException(nameof(EnquiryAssignmentEventHandler), "ASSIGNED_CONSULTANT_EMAIL_INVALID",
                    new InvalidOperationException($"Consultant '{consultant.UserName}' has an invalid email address. Enquiry ID: {savedEnquiry.ItemID}."));
                return;
            }

            var assignmentEmailService = new EnquiryAssignmentEmailService(
                emailService,
                users,
                configuration,
                eventLog);

            bool sent = await assignmentEmailService.SendAssignmentEmailAsync(savedEnquiry, newAssigneeGuid);
            if (!sent)
            {
                // The service has already written the exact failing stage to the Event Log.
                return;
            }
        }
        catch (Exception ex)
        {
            eventLog.LogException(nameof(EnquiryAssignmentEventHandler), "ASSIGNMENT_HANDLER_FAILED", ex);
        }
    }
}
