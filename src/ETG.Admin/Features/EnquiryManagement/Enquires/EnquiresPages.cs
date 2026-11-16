using CMS.Core;
using Kentico.Xperience.Admin.Base.Authentication;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using CMS.OnlineForms;
using CMS.OnlineForms.Types;
using ETG.Admin.Features.EnquiryManagement.Enquires;
using ETG.Core.Content.Forms;
using Kentico.Xperience.Admin.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CMS;
using ETG.Core.Modules;

[assembly: UIPage(
    parentType: typeof(
        ETG.Admin.Features.EnquiryManagement
            .EnquiryManagementApplicationPage),
    slug: "enquires",
    uiPageType: typeof(EnquiresPage),
    name: "Enquires",
    templateName: EnquiresPage.TEMPLATE_NAME,
    order: 2)]

[assembly: UIPage(
    parentType: typeof(EnquiresPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(EnquiryEditSection),
    name: "Enquiry",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(EnquiryEditSection),
    slug: "edit",
    uiPageType: typeof(EnquiryEditPage),
    name: "Edit Enquiry",
    templateName: "@website/web-admin/EnquiryEdit",
    order: UIPageOrder.NoOrder)]

[assembly: RegisterImplementation(typeof(ETG.Admin.Features.EnquiryManagement.Enquires.IEnquiryWorkflow),
    typeof(ETG.Admin.Features.EnquiryManagement.Enquires.EnquiryWorkflow))]

namespace ETG.Admin.Features.EnquiryManagement.Enquires;

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class EnquiresPage : Page
{
    private readonly IEventLogService eventLogService;
    private readonly IUserInfoProvider users;
    private readonly IPageLinkGenerator pageLinkGenerator;
    private readonly IAuthenticatedUserAccessor userAccessor;
    protected readonly IEnquiryWorkflow workflow;
    protected UserInfo CurrentUser { get; private set; } = null!;

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();
        CurrentUser = await userAccessor.Get();
    }

    protected EnquireItem? GetAccessibleEnquiry(int id) => id <= 0 ? null
        : workflow.ApplyAccess(BizFormItemProvider.GetItems<EnquireItem>(), CurrentUser)
            .WhereEquals(GetIdColumn(), id).FirstOrDefault();


    public EnquiresPage(
        IEventLogService eventLogService,
        IUserInfoProvider users,
        IPageLinkGenerator pageLinkGenerator,
        IAuthenticatedUserAccessor userAccessor,
        IEnquiryWorkflow workflow)
    {
        this.userAccessor = userAccessor;
        this.workflow = workflow;
        this.users = users;
        this.pageLinkGenerator = pageLinkGenerator;
        this.eventLogService = eventLogService
            ?? throw new ArgumentNullException(nameof(eventLogService));
    }


    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<ManagementOptions>> GetManagementOptions() =>
        Task.FromResult(ResponseFrom(new ManagementOptions
        {
            Statuses = EnquiryStatuses.Values,
            Consultants = workflow.GetConsultants(""),
            CanDelete = workflow.CanDelete(CurrentUser),
            CanViewTracking = workflow.CanViewTracking(CurrentUser)
        }));

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse<ConsultantChoicesResult>> GetConsultants(ConsultantChoicesArguments arguments)
    {
        var item = GetAccessibleEnquiry(arguments.ItemId);
        if (item is null || !CanUseDestination(arguments.Destination))
            return Task.FromResult(ResponseFrom(new ConsultantChoicesResult { Success = false }));
        return Task.FromResult(ResponseFrom(new ConsultantChoicesResult
        {
            Success = true,
            Destination = arguments.Destination,
            Consultants = workflow.GetConsultants(arguments.Destination)
        }));
    }

    private bool CanUseDestination(string destination) => workflow.CanUseDestination(CurrentUser, destination);

    private void ValidateAssignee(string value, Guid previous, string destination)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!Guid.TryParse(value, out var guid)) throw new ArgumentException("Select a valid consultant.");
        if (guid == previous) return; // Preserve legacy assignments even if a role was subsequently removed.
        if (!workflow.GetConsultants(destination).Any(x => Guid.Parse(x.Value) == guid))
            throw new ArgumentException("The selected consultant is not eligible for this destination.");
    }

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public async Task<ICommandResponse<SaveEnquiryResult>> UpdateManagement(ManagementUpdateArguments arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = GetAccessibleEnquiry(arguments.ItemId);
            if (item is null) return await SaveFailure("Enquiry not found or access denied.");
            Guid previous = ValidationHelper.GetGuid(item.GetValue("AssignedUserGuid"), Guid.Empty);
            if (arguments.Field == "status")
            {
                EnquiryStatuses.Validate(arguments.Value, item.Status, item.BookingReference,
                    ReadDate(item, "DepartureDate"), ReadDate(item, "ReturnDate"));
                item.Status = arguments.Value;
            }
            else if (arguments.Field == "consultant")
            {
                ValidateAssignee(arguments.Value, previous, item.PreferredDestination);
                item.SetValue("AssignedUserGuid", string.IsNullOrWhiteSpace(arguments.Value) ? null : Guid.Parse(arguments.Value));
            }
            else return await SaveFailure("Unsupported enquiry field.");
            item.Update();

            // Send only when the consultant assignment actually changed.
            // This custom Admin PageCommand is the real save path for inline updates.
            await EnquiryAssignmentEventHandler.HandleAssignmentChangedAsync(
                item,
                previous,
                users,
                Service.Resolve<CMS.EmailEngine.IEmailService>(),
                Service.Resolve<Microsoft.Extensions.Configuration.IConfiguration>(),
                eventLogService);

            return ResponseFrom(SaveEnquiryResult.SuccessResult(
                MapRows([item])[0], updatedField: arguments.Field));
        }
        catch (OperationCanceledException) { throw; }
        catch (ArgumentException ex) { return await SaveFailure(ex.Message); }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(EnquiresPage), "UPDATEMANAGEMENT", ex);
            return await SaveFailure("Unable to update enquiry. Check the Event log.");
        }
    }

    // Organization: Website
    // Client project: web-admin
    // React export: EnquiresTemplate
    public const string TEMPLATE_NAME =
        "@website/web-admin/Enquires";

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<INavigateResponse> OpenEnquiry(int id)
    {
        if (id <= 0) throw new ArgumentException("A valid enquiry ID is required.");
        if (GetAccessibleEnquiry(id) is null) throw new UnauthorizedAccessException("Enquiry not found or access denied.");
        var parameters = new PageParameterValues
        {
            { typeof(EnquiryEditSection), id }
        };
        return Task.FromResult(NavigateTo(
            pageLinkGenerator.GetPath(typeof(EnquiryEditPage), parameters)));
    }

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<INavigateResponse> ReturnToEnquiries() =>
        Task.FromResult(NavigateTo(pageLinkGenerator.GetPath(typeof(EnquiresPage))));

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<EnquiryListResult>> GetEnquiries(
        EnquiryListArguments arguments, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (arguments is null) return ListFailure("List arguments are required.");
            const int pageSize = 25;
            int page = Math.Clamp(arguments.Page, 1, int.MaxValue / pageSize);
            var query = BuildQuery(arguments).Page(page - 1, pageSize);
            query.GetTotalRecordsForPagedQuery = true;
            var records = query.ToList();
            int total = query.TotalRecords;
            int last = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            if (page > last)
            {
                page = last;
                records = BuildQuery(arguments).Page(page - 1, pageSize).ToList();
            }
            cancellationToken.ThrowIfCancellationRequested();
            var items = MapRows(records);
            return Task.FromResult(ResponseFrom(
                EnquiryListResult.SuccessResult(items, total, page, pageSize)));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(EnquiresPage), "GETENQUIRIES", ex);
            return ListFailure("Unable to load enquiries. Check the Event log.");
        }
    }

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public virtual Task<ICommandResponse<SaveEnquiryResult>> UpdateEnquiry(
        SaveEnquiryArguments arguments, CancellationToken cancellationToken) =>
        SaveEnquiryInternal(arguments, cancellationToken);

    [PageCommand(Permission = SystemPermissions.VIEW)]
    public Task<ICommandResponse<EnquiryExportResult>> ExportEnquiries(
        CancellationToken cancellationToken)
    {
        try
        {
            // Export includes all accessible enquiries, regardless of listing search/filter.
            var exportArguments = new EnquiryListArguments
            {
                SearchText = string.Empty,
                SortColumn = "FormInserted",
                SortDescending = true
            };
            IEnumerable<EnquiryDto> ReadRows()
            {
                const int batchSize = 500;
                for (int index = 0; ; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var batch = BuildQuery(exportArguments).Page(index, batchSize).ToList();
                    foreach (var row in MapRows(batch)) yield return row;
                    if (batch.Count < batchSize) yield break;
                }
            }
            byte[] bytes = EnquiryExcelExporter.Build(ReadRows(), cancellationToken);
            return Task.FromResult(ResponseFrom(new EnquiryExportResult
            {
                Success = true,
                FileName = $"Enquiries-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx",
                Base64 = Convert.ToBase64String(bytes)
            }));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            eventLogService.LogException(nameof(EnquiresPage), "EXPORTENQUIRIES", ex);
            return Task.FromResult(ResponseFrom(new EnquiryExportResult
            {
                Errors = new[] { "Unable to export enquiries. Check the Event log." }
            }));
        }
    }

    private ObjectQuery<EnquireItem> BuildQuery(EnquiryListArguments arguments)
    {
        var query = workflow.ApplyAccess(BizFormItemProvider.GetItems<EnquireItem>(), CurrentUser);
        string text = Clean(arguments.SearchText);
        if (text.Length > 200) throw new ArgumentException("Search must be 200 characters or fewer.");
        if (text.Length > 0)
        {
            query = query.Where(new WhereCondition()
                .WhereContains("PreferredDestination", text).Or()
                .WhereContains("Status", text).Or()
                .WhereContains("Firstname", text).Or()
                .WhereContains("Lastname", text).Or()
                .WhereContains("Email", text).Or()
                .WhereContains("Phone", text).Or()
                .WhereContains("PreferredExperience", text).Or()
                .WhereContains("TourName", text).Or()
                .WhereContains("TourCode", text));
        }
        if (!string.IsNullOrWhiteSpace(arguments.ConsultantGuid))
        {
            if (!Guid.TryParse(arguments.ConsultantGuid, out var consultant))
                throw new ArgumentException("Select a valid consultant filter.");
            query = query.WhereEquals("AssignedUserGuid", consultant);
        }
        string id = GetIdColumn();
        string column = GetSortColumn(arguments.SortColumn, id);
        string direction = arguments.SortDescending ? "DESC" : "ASC";
        return query.OrderBy(column == id
            ? $"{column} {direction}"
            : $"{column} {direction}, {id} DESC");
    }

    protected List<EnquiryDto> MapRows(List<EnquireItem> rows)
    {
        var ids = rows.Select(x => ValidationHelper.GetGuid(
            x.GetValue("AssignedUserGuid"), Guid.Empty))
            .Where(x => x != Guid.Empty).Distinct().ToArray();
        var names = new Dictionary<Guid, string>();
        if (ids.Length > 0)
        {
            foreach (var user in users.Get().WhereIn("UserGUID", ids).ToList())
            {
                string name = ValidationHelper.GetString(user.GetValue("FullName"), "").Trim();
                if (name.Length == 0)
                    name = $"{ValidationHelper.GetString(user.GetValue("FirstName"), "")} {ValidationHelper.GetString(user.GetValue("LastName"), "")}".Trim();
                names[user.UserGUID] = name.Length > 0 ? name : user.UserName;
            }
        }
        var choices = rows.Select(x => x.PreferredDestination).Distinct()
            .ToDictionary(x => x, x => workflow.GetConsultants(x));
        return rows.Select(row =>
        {
            var dto = MapToDto(row);
            Guid guid = ValidationHelper.GetGuid(row.GetValue("AssignedUserGuid"), Guid.Empty);
            dto.ConsultantName = guid == Guid.Empty ? ""
                : names.TryGetValue(guid, out string? name) ? name
                : $"Unknown consultant ({guid})";
            dto.Consultants = choices[row.PreferredDestination];
            if (!workflow.CanViewTracking(CurrentUser))
                dto.UTMCampaign = dto.UTMContent = dto.UTMMedium = dto.UTMSource = dto.UTMTerm = "";
            return dto;
        }).ToList();
    }

    private async Task<ICommandResponse<SaveEnquiryResult>> SaveEnquiryInternal(
        SaveEnquiryArguments arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (arguments?.Item is null)
            {
                return await SaveFailure(
                    "Enquiry data was not provided.");
            }

            EnquiryDto model = arguments.Item;

            if (model.ItemId <= 0)
            {
                return await SaveFailure(
                    "A valid enquiry ID is required.");
            }

            List<string> errors = Validate(model);

            if (errors.Count > 0)
            {
                ICommandResponse<SaveEnquiryResult> invalidResponse =
                    ResponseFrom(
                            SaveEnquiryResult.Failure(errors))
                        .AddErrorMessage(
                            "Please correct the enquiry fields.");

                return invalidResponse;
            }

            EnquireItem? item = GetAccessibleEnquiry(model.ItemId);

            if (item is null)
            {
                return await SaveFailure(
                    "The enquiry was not found.");
            }

            string status = string.IsNullOrWhiteSpace(model.Status) ? "New" : model.Status.Trim();
            EnquiryStatuses.Validate(status, item.Status, model.BookingReference, model.DepartureDate, model.ReturnDate);
            // Restricted users cannot move an enquiry outside their permitted destinations.
            if (model.PreferredDestination != item.PreferredDestination && !CanUseDestination(model.PreferredDestination))
                return await SaveFailure("You cannot move this enquiry to that destination.");
            Guid previousAssignee = ValidationHelper.GetGuid(item.GetValue("AssignedUserGuid"), Guid.Empty);
            ValidateAssignee(model.AssignedUserGuid, previousAssignee, model.PreferredDestination);
            if (!workflow.CanViewTracking(CurrentUser))
            {
                model.UTMCampaign = item.UTMCampaign; model.UTMContent = item.UTMContent;
                model.UTMMedium = item.UTMMedium; model.UTMSource = item.UTMSource; model.UTMTerm = item.UTMTerm;
            }
            MapToItem(model, item);

            cancellationToken.ThrowIfCancellationRequested();

            item.Update();

            // Send only when the consultant assignment actually changed.
            // This covers the full Edit Enquiry form save command.
            await EnquiryAssignmentEventHandler.HandleAssignmentChangedAsync(
                item,
                previousAssignee,
                users,
                Service.Resolve<CMS.EmailEngine.IEmailService>(),
                Service.Resolve<Microsoft.Extensions.Configuration.IConfiguration>(),
                eventLogService);

            return ResponseFrom(SaveEnquiryResult.SuccessResult(MapRows([item])[0]));
        }
        catch (ArgumentException ex)
        {
            return await SaveFailure(ex.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            eventLogService.LogException(
                 nameof(EnquiresPage),
                "UPDATEENQUIRY",
                exception);

            return await SaveFailure(
                "Unable to save the enquiry. Check the Event log.");
        }
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public Task<ICommandResponse<DeleteEnquiryResult>> DeleteEnquiry(
        DeleteEnquiryArguments arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (arguments is null || arguments.ItemId <= 0)
            {
                return DeleteFailure(
                    "A valid enquiry ID is required.");
            }

            if (!workflow.CanDelete(CurrentUser)) return DeleteFailure("Only administrators, managers and team leaders can delete enquiries.");
            EnquireItem? item = GetAccessibleEnquiry(arguments.ItemId);

            if (item is null)
            {
                return DeleteFailure(
                    "The enquiry was not found.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            item.Delete();

            return Task.FromResult(ResponseFrom(DeleteEnquiryResult.SuccessResult()));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            eventLogService.LogException(
                 nameof(EnquiresPage),
                "DELETEENQUIRY",
                exception);

            return DeleteFailure(
                "Unable to delete the enquiry. Check the Event log.");
        }
    }

    private Task<ICommandResponse<EnquiryListResult>> ListFailure(
        string message)
    {
        ICommandResponse<EnquiryListResult> response =
            ResponseFrom(
                    EnquiryListResult.Failure(message))
                .AddErrorMessage(message);

        return Task.FromResult(response);
    }

    private Task<ICommandResponse<SaveEnquiryResult>> SaveFailure(
        string message)
    {
        ICommandResponse<SaveEnquiryResult> response =
            ResponseFrom(
                    SaveEnquiryResult.Failure(message))
                .AddErrorMessage(message);

        return Task.FromResult(response);
    }

    private Task<ICommandResponse<DeleteEnquiryResult>> DeleteFailure(
        string message)
    {
        ICommandResponse<DeleteEnquiryResult> response =
            ResponseFrom(
                    DeleteEnquiryResult.Failure(message))
                .AddErrorMessage(message);

        return Task.FromResult(response);
    }



    private static string GetIdColumn()
    {
        string idColumn = BizFormItemProvider
            .GetTypeInfo(EnquireItem.CLASS_NAME)
            .IDColumn;

        if (string.IsNullOrWhiteSpace(idColumn))
        {
            throw new InvalidOperationException(
                "The Enquire form primary key could not be resolved.");
        }

        return idColumn;
    }

    private static string GetSortColumn(string? column, string idColumn)
    {
        return Clean(column).ToLowerInvariant() switch
        {
            "itemid" => idColumn,
            "firstname" => nameof(EnquireItem.Firstname),
            "lastname" => nameof(EnquireItem.Lastname),
            "email" => nameof(EnquireItem.Email),
            "phone" => nameof(EnquireItem.Phone),
            "tourcode" => nameof(EnquireItem.TourCode),
            "tourname" => nameof(EnquireItem.TourName),
            "status" => nameof(EnquireItem.Status),
            "preferreddestination" => "PreferredDestination",
            "preferredcontactmethod" => "PreferredContactMethod",
            "preferredexperience" => "PreferredExperience",
            "utmcampaign" => "UTMCampaign",
            "utmcontent" => "UTMContent",
            "utmmedium" => "UTMMedium",
            "utmsource" => "UTMSource",
            "utmterm" => "UTMTerm",
            "formupdated" => "FormUpdated",
            "bookingreference" =>
                nameof(EnquireItem.BookingReference),
            _ => "FormInserted"
        };
    }

    private static List<string> Validate(EnquiryDto model)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(model.FirstName))
        {
            errors.Add("First Name is required.");
        }

        if (string.IsNullOrWhiteSpace(model.LastName))
        {
            errors.Add("Last Name is required.");
        }

        if (string.IsNullOrWhiteSpace(model.Email))
        {
            errors.Add("Email is required.");
        }
        else if (!ValidationHelper.IsEmail(Clean(model.Email)))
        {
            errors.Add("Enter a valid email address.");
        }

        if (model.TourQuotedPrice < 0)
        {
            errors.Add("Quoted Price cannot be negative.");
        }

        return errors;
    }

    protected static EnquiryDto MapToDto(EnquireItem item)
    {
        return new EnquiryDto
        {
            ItemId = item.ItemID,

            FormInserted = item.FormInserted == DateTime.MinValue
                ? string.Empty
                : item.FormInserted.ToString("yyyy-MM-dd HH:mm:ss"),

            FormUpdated = item.FormUpdated == DateTime.MinValue
                ? string.Empty
                : item.FormUpdated.ToString("yyyy-MM-dd HH:mm:ss"),

            FirstName = item.Firstname,
            LastName = item.Lastname,
            Email = item.Email,
            Phone = item.Phone,

            PreferredContactMethod = item.PreferredContactMethod,
            PreferredTourType = item.PreferredTourType,
            PreferredDestination = item.PreferredDestination,
            PreferredExperience = item.PreferredExperience,

            TourCode = item.TourCode,
            TourName = item.TourName,
            TourLink = item.TourLink,
            Message = item.Message,
            TourDate = item.TourDate,
            TourClass = item.TourClass,
            TourDepartureCity = item.TourDepartureCity,
            TourDuration = item.TourDuration,
            TourQuotedPrice = item.TourQuotedPrice,
            PriceCurrency = item.PriceCurrency,
            TourDiscountText = item.TourDiscountText,

            Travellers = item.Travellers,
            Children = ValidationHelper.GetString(item.GetValue("Children"), string.Empty),
            Infants = item.Infants,
            SubscribeToNewsletter = item.SubscribeToNewsletter,

            UTMCampaign = item.UTMCampaign,
            UTMContent = item.UTMContent,
            UTMMedium = item.UTMMedium,
            UTMSource = item.UTMSource,
            UTMTerm = item.UTMTerm,

            Comments = item.Comments,
            Status = item.Status,
            BookingReference = item.BookingReference,
            DepartureDate = ReadDate(item, "DepartureDate"),
            ReturnDate = ReadDate(item, "ReturnDate"),
            AssignedUserGuid = ReadGuid(item, "AssignedUserGuid"),
            StoreView = item.StoreView,
            IsSpam = item.IsSpam
        };
    }

    private static void MapToItem(
        EnquiryDto model,
        EnquireItem item)
    {
        item.Firstname = Clean(model.FirstName);
        item.Lastname = Clean(model.LastName);
        item.Email = Clean(model.Email);
        item.Phone = Clean(model.Phone);

        item.PreferredContactMethod =
            Clean(model.PreferredContactMethod);

        item.PreferredTourType =
            Clean(model.PreferredTourType);

        item.PreferredDestination =
            Clean(model.PreferredDestination);

        item.PreferredExperience =
            Clean(model.PreferredExperience);

        item.TourCode = Clean(model.TourCode);
        item.TourName = Clean(model.TourName);
        item.TourLink = Clean(model.TourLink);
        item.Message = Clean(model.Message);
        item.TourDate = Clean(model.TourDate);
        item.TourClass = Clean(model.TourClass);

        item.TourDepartureCity =
            Clean(model.TourDepartureCity);

        item.TourDuration = Clean(model.TourDuration);
        item.TourQuotedPrice = model.TourQuotedPrice;
        item.PriceCurrency = Clean(model.PriceCurrency);

        item.TourDiscountText =
            Clean(model.TourDiscountText);

        item.Travellers = Clean(model.Travellers);
        item.SetValue("Children", Clean(model.Children));
        item.Infants = Clean(model.Infants);

        item.SubscribeToNewsletter =
            model.SubscribeToNewsletter;

        item.UTMCampaign = Clean(model.UTMCampaign);
        item.UTMContent = Clean(model.UTMContent);
        item.UTMMedium = Clean(model.UTMMedium);
        item.UTMSource = Clean(model.UTMSource);
        item.UTMTerm = Clean(model.UTMTerm);

        item.Comments = Clean(model.Comments);

        item.Status = string.IsNullOrWhiteSpace(model.Status)
            ? "New"
            : model.Status.Trim();

        item.BookingReference =
            Clean(model.BookingReference);

        item.SetValue("DepartureDate", ParseDate(model.DepartureDate));
        item.SetValue("ReturnDate", ParseDate(model.ReturnDate));

        if (string.IsNullOrWhiteSpace(model.AssignedUserGuid))
        {
            item.SetValue("AssignedUserGuid", null);
        }
        else if (Guid.TryParse(model.AssignedUserGuid, out Guid assignedGuid))
        {
            item.SetValue("AssignedUserGuid", assignedGuid);
        }
        else
        {
            throw new ArgumentException("Assigned Consultant GUID must be a valid GUID.");
        }

        item.StoreView = Clean(model.StoreView);
        item.IsSpam = model.IsSpam;
    }

    private static string ReadDate(EnquireItem item, string field)
    {
        object? value = item.GetValue(field);

        // Kentico may return an empty string for an unset date field.
        // Convert.ToDateTime("") throws FormatException and prevents the
        // entire enquiries list from being mapped.
        if (value is null || value == DBNull.Value)
        {
            return string.Empty;
        }

        if (value is DateTime dateValue)
        {
            return dateValue == DateTime.MinValue
                ? string.Empty
                : dateValue.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        string rawValue = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim()
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        // Be tolerant of legacy values while keeping the API date format stable.
        if (DateTime.TryParse(rawValue, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces, out DateTime parsedDate))
        {
            return parsedDate == DateTime.MinValue
                ? string.Empty
                : parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        // An invalid optional date should not prevent other enquiries from loading.
        return string.Empty;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateTime.TryParseExact(value.Trim(), "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            return date;
        throw new ArgumentException("Enter dates in yyyy-MM-dd format.");
    }

    private static string ReadGuid(EnquireItem item, string field)
    {
        Guid value = ValidationHelper.GetGuid(item.GetValue(field), Guid.Empty);
        return value == Guid.Empty ? string.Empty : value.ToString();
    }

    private static string Clean(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}

// Command arguments

public sealed class EnquiryListArguments
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string SearchText { get; set; } = string.Empty;
    public string ConsultantGuid { get; set; } = string.Empty;
    public string SortColumn { get; set; } = "FormInserted";
    public bool SortDescending { get; set; } = true;
}

public sealed class SaveEnquiryArguments
{
    public EnquiryDto? Item { get; set; }
}

public sealed class DeleteEnquiryArguments
{
    public int ItemId { get; set; }
}

// Command results

public sealed class EnquiryListResult
{
    public bool Success { get; init; }
    public IReadOnlyList<EnquiryDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public static EnquiryListResult SuccessResult(
        IReadOnlyList<EnquiryDto> items,
        int totalCount,
        int page,
        int pageSize)
    {
        return new EnquiryListResult
        {
            Success = true,
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public static EnquiryListResult Failure(params string[] errors)
    {
        return new EnquiryListResult
        {
            Success = false,
            Errors = errors
        };
    }
}

public sealed class SaveEnquiryResult
{
    public bool Success { get; init; }
    public EnquiryDto? Item { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public string UpdatedField { get; init; } = string.Empty;

    public static SaveEnquiryResult SuccessResult(EnquiryDto item, string updatedField = "")
    {
        return new SaveEnquiryResult
        {
            Success = true,
            Item = item,
            UpdatedField = updatedField
        };
    }

    public static SaveEnquiryResult Failure(params string[] errors)
    {
        return new SaveEnquiryResult
        {
            Success = false,
            Errors = errors
        };
    }

    public static SaveEnquiryResult Failure(IEnumerable<string> errors)
    {
        return new SaveEnquiryResult
        {
            Success = false,
            Errors = errors.ToList()
        };
    }
}

public sealed class DeleteEnquiryResult
{
    public bool Success { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
    public static DeleteEnquiryResult SuccessResult()
    {
        return new DeleteEnquiryResult
        {
            Success = true
        };
    }

    public static DeleteEnquiryResult Failure(params string[] errors)
    {
        return new DeleteEnquiryResult
        {
            Success = false,
            Errors = errors
        };
    }
}

// Shared DTO

public sealed class EnquiryDto
{
    public int ItemId { get; set; }
    public string ConsultantName { get; set; } = string.Empty;
    public IReadOnlyList<ConsultantOption> Consultants { get; set; } = [];
    public string FormInserted { get; set; } = string.Empty;
    public string FormUpdated { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PreferredContactMethod { get; set; } = string.Empty;
    public string PreferredTourType { get; set; } = string.Empty;
    public string PreferredDestination { get; set; } = string.Empty;
    public string PreferredExperience { get; set; } = string.Empty;
    public string TourCode { get; set; } = string.Empty;
    public string TourName { get; set; } = string.Empty;
    public string TourLink { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string TourDate { get; set; } = string.Empty;
    public string TourClass { get; set; } = string.Empty;
    public string TourDepartureCity { get; set; } = string.Empty;
    public string TourDuration { get; set; } = string.Empty;
    public int TourQuotedPrice { get; set; }
    public string PriceCurrency { get; set; } = string.Empty;
    public string TourDiscountText { get; set; } = string.Empty;
    public string Travellers { get; set; } = string.Empty;
    public string Children { get; set; } = string.Empty;
    public string Infants { get; set; } = string.Empty;
    public bool SubscribeToNewsletter { get; set; }
    public string UTMCampaign { get; set; } = string.Empty;
    public string UTMContent { get; set; } = string.Empty;
    public string UTMMedium { get; set; } = string.Empty;
    public string UTMSource { get; set; } = string.Empty;
    public string UTMTerm { get; set; } = string.Empty;
    public string Comments { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BookingReference { get; set; } = string.Empty;
    public string DepartureDate { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public string AssignedUserGuid { get; set; } = string.Empty;
    public string StoreView { get; set; } = string.Empty;
    public bool IsSpam { get; set; }
}
public sealed class EnquiryExportResult
{
    public bool Success { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string Base64 { get; init; } = string.Empty;
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class EnquiryEditSection : Page
{
    [PageParameter(typeof(IntPageModelBinder))]
    public int EnquiryId { get; set; }

    public override async Task<TemplateClientProperties> ConfigureTemplateProperties(
        TemplateClientProperties properties)
    {
        properties = await base.ConfigureTemplateProperties(properties);
        properties.Breadcrumbs.Show = false;
        properties.Navigation ??= new Navigation();
        properties.Navigation.Headline = EnquiryId.ToString(CultureInfo.InvariantCulture);
        properties.Navigation.ShowHeadline = true;
        return properties;
    }
}

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class EnquiryEditPage : EnquiresPage
{
    public EnquiryEditPage(
        IEventLogService eventLogService,
        IUserInfoProvider users,
        IPageLinkGenerator pageLinkGenerator,
        IAuthenticatedUserAccessor userAccessor,
        IEnquiryWorkflow workflow)
        : base(
            eventLogService,
            users,
            pageLinkGenerator,
            userAccessor,
            workflow)
    {
    }

    [PageParameter(typeof(IntPageModelBinder), typeof(EnquiryEditSection))]
    public int EnquiryId { get; set; }

    public override async Task<TemplateClientProperties> ConfigureTemplateProperties(
        TemplateClientProperties properties)
    {
        properties = await base.ConfigureTemplateProperties(properties);
        properties.Breadcrumbs.Label = $"Edit Enquiry {EnquiryId}";
        properties.Breadcrumbs.Show = true;
        return properties;
    }

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public Task<ICommandResponse<SaveEnquiryResult>> LoadEnquiry(
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = GetAccessibleEnquiry(EnquiryId);
            return Task.FromResult(ResponseFrom(item is null
                ? SaveEnquiryResult.Failure("The enquiry was not found.")
                : SaveEnquiryResult.SuccessResult(MapRows([item])[0])));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            EventLogService.LogException(nameof(EnquiryEditPage), "LOADENQUIRY", ex);
            return Task.FromResult(ResponseFrom(SaveEnquiryResult.Failure(
                "Unable to load the enquiry. Check the Event log.")));
        }
    }

    [PageCommand(Permission = SystemPermissions.UPDATE)]
    public override Task<ICommandResponse<SaveEnquiryResult>> UpdateEnquiry(
        SaveEnquiryArguments arguments, CancellationToken cancellationToken)
    {
        if (EnquiryId <= 0 || arguments?.Item?.ItemId != EnquiryId)
            return Task.FromResult(ResponseFrom(SaveEnquiryResult.Failure(
                "The enquiry ID does not match the edit page.")));
        return base.UpdateEnquiry(arguments, cancellationToken);
    }
}

public sealed class ManagementOptions
{
    public IReadOnlyList<string> Statuses { get; init; } = [];
    public IReadOnlyList<ConsultantOption> Consultants { get; init; } = [];
    public bool CanDelete { get; init; }
    public bool CanViewTracking { get; init; }
}
public sealed class ManagementUpdateArguments
{
    public int ItemId { get; set; }
    public string Field { get; set; } = "";
    public string Value { get; set; } = "";
}
public sealed class ConsultantChoicesArguments
{
    public int ItemId { get; set; }
    public string Destination { get; set; } = "";
}
public sealed class ConsultantChoicesResult
{
    public bool Success { get; init; }
    public string Destination { get; init; } = "";
    public IReadOnlyList<ConsultantOption> Consultants { get; init; } = [];
}

public static class EnquiryStatuses
{
    public static readonly string[] Values = ["New", "1st contact", "2nd contact", "WIP", "Cancelled", "Booked"];

    public static void Validate(string value, string original, string bookingReference,
        string departureDate, string returnDate)
    {
        // Preserve an existing legacy value, including Confirmed, without offering new unsupported values.
        if (!Values.Contains(value, StringComparer.Ordinal) && value != original)
            throw new ArgumentException("Select a valid enquiry status.");
        if (value == "Confirmed" && (string.IsNullOrWhiteSpace(bookingReference)
            || string.IsNullOrWhiteSpace(departureDate) || string.IsNullOrWhiteSpace(returnDate)))
            throw new ArgumentException("Booking Reference, Departure date, Return date cannot be null.");
    }
}

public interface IEnquiryWorkflow
{
    ObjectQuery<EnquireItem> ApplyAccess(ObjectQuery<EnquireItem> query, UserInfo user);
    bool CanUseDestination(UserInfo user, string destination);
    bool CanDelete(UserInfo user);
    bool CanViewTracking(UserInfo user);
    IReadOnlyList<ConsultantOption> GetConsultants(string destination);
}

public sealed record ConsultantOption(string Value, string Label);

public sealed class EnquiryWorkflow(
    IUserInfoProvider users,
    IRoleInfoProvider roles,
    IUserRoleInfoProvider userRoles,
    IInfoProvider<RoleToDestinationLinkInfo> links) : IEnquiryWorkflow
{
    private const string GeneralRole = "EnquiryManagementUser";
    private static bool HasRole(UserInfo user, string role) => user.IsInRole(role);

    public bool CanDelete(UserInfo user) => user.IsAdministrator() || HasRole(user, "Manager") || HasRole(user, "TeamLeader");
    public bool CanViewTracking(UserInfo user) => user.IsAdministrator() || HasRole(user, "Manager")
        || HasRole(user, "Marketing") || HasRole(user, GeneralRole);

    public ObjectQuery<EnquireItem> ApplyAccess(ObjectQuery<EnquireItem> query, UserInfo user)
    {
        if (user.IsAdministrator() || HasRole(user, "Manager")) return query;
        if (!HasRole(user, "Consultant") && !HasRole(user, "TeamLeader")) return query;
        var roleIds = userRoles.Get().WhereEquals("UserID", user.UserID).ToList().Select(x => x.RoleID).ToArray();
        if (roleIds.Length == 0) return query.Where("1 = 2");
        var roleGuids = roles.Get().WhereIn("RoleID", roleIds).ToList().Select(x => x.RoleGUID).ToArray();
        var patterns = links.Get().ToList().Where(x => roleGuids.Contains(x.RoleDestinationRoleGuid))
            .Select(x => x.RoleDestinationDestinations).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        if (patterns.Count == 0) return query.Where("1 = 2");
        var condition = new WhereCondition();
        bool first = true;
        foreach (string pattern in patterns)
        {
            if (!first) condition.Or();
            first = false;
            // K13 uses the complete stored value as a LIKE pattern; do not split or add wildcards.
            condition.WhereLike("PreferredDestination", pattern);
        }
        return query.Where(condition);
    }

    public bool CanUseDestination(UserInfo user, string destination)
    {
        if (user.IsAdministrator() || HasRole(user, "Manager") ||
            (!HasRole(user, "Consultant") && !HasRole(user, "TeamLeader"))) return true;
        var roleIds = userRoles.Get().WhereEquals("UserID", user.UserID).ToList().Select(x => x.RoleID).ToArray();
        if (roleIds.Length == 0) return false;
        var roleGuids = roles.Get().WhereIn("RoleID", roleIds).ToList().Select(x => x.RoleGUID).ToArray();
        var patterns = links.Get().ToList().Where(x => roleGuids.Contains(x.RoleDestinationRoleGuid))
            .Select(x => x.RoleDestinationDestinations).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct();
        foreach (string pattern in patterns)
        {
            var parameters = new QueryDataParameters { { "Destination", destination }, { "Pattern", pattern } };
            if (ValidationHelper.GetInteger(ConnectionHelper.ExecuteScalar(
                "SELECT CASE WHEN @Destination LIKE @Pattern THEN 1 ELSE 0 END", parameters, QueryTypeEnum.SQLQuery), 0) == 1)
                return true;
        }
        return false;
    }

    public IReadOnlyList<ConsultantOption> GetConsultants(string destination)
    {
        var allRoles = roles.Get().ToList();
        var mappedGuids = string.IsNullOrEmpty(destination) ? [] : links.Get().ToList()
            .Where(x => x.RoleDestinationName == destination).Select(x => x.RoleDestinationRoleGuid).ToArray();
        var roleIds = allRoles.Where(x => x.RoleName == GeneralRole || mappedGuids.Contains(x.RoleGUID))
            .Select(x => x.RoleID).ToArray();
        if (roleIds.Length == 0) return [];
        var userIds = userRoles.Get().WhereIn("RoleID", roleIds).ToList().Select(x => x.UserID).Distinct().ToArray();
        if (userIds.Length == 0) return [];
        return users.Get().WhereIn("UserID", userIds).WhereEquals("UserEnabled", true).ToList()
            .Where(x => x.UserAdministrationAccess)
            .Select(x => new ConsultantOption(x.UserGUID.ToString(), DisplayName(x)))
            .OrderBy(x => x.Label, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static string DisplayName(UserInfo user)
    {
        string name = ValidationHelper.GetString(user.GetValue("FullName"), "").Trim();
        if (name.Length == 0) name = $"{user.FirstName} {user.LastName}".Trim();
        return name.Length == 0 ? user.UserName : name;
    }


}
