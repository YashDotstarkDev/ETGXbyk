using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;
using ETG.ContactTags;
using ETG.Core.Modules;
using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.Forms;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using CMS.Helpers;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ContactTags.ContactTagsApplicationPage),
    slug: "contact-tags",
    uiPageType: typeof(ETG.Admin.Features.ETG.ContactTags.ContactTagsListPage),
    name: "Contact Tags",
    templateName: TemplateNames.LISTING,
    order: 3)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETG.ContactTags.ContactTagsListPage),
    slug: "create",
    uiPageType: typeof(ETG.Admin.Features.ETG.ContactTags.ContactTagsCreatePage),
    name: "CREATE",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETG.ContactTags.ContactTagsListPage),
    slug: PageParameterConstants.PARAMETERIZED_SLUG,
    uiPageType: typeof(ETG.Admin.Features.ETG.ContactTags.ContactTagsEditSection),
    name: "Edit",
    templateName: TemplateNames.SECTION_LAYOUT,
    order: UIPageOrder.NoOrder)]

[assembly: UIPage(
    parentType: typeof(ETG.Admin.Features.ETG.ContactTags.ContactTagsEditSection),
    slug: "edit",
    uiPageType: typeof(ETG.Admin.Features.ETG.ContactTags.ContactTagsEditPage),
    name: "Edit",
    templateName: TemplateNames.EDIT,
    order: UIPageOrder.NoOrder)]

namespace ETG.Admin.Features.ETG.ContactTags;

internal static class ContactTagsPageSettings
{
    public const string FormName = "create";
}

[UIEvaluatePermission(SystemPermissions.VIEW)]
public class ContactTagsListPage(IConfiguration configuration) : ListingPage
{
    private readonly string connectionString =
        configuration.GetConnectionString("CMSConnectionString")
        ?? configuration["CMSConnectionString"]
        ?? throw new InvalidOperationException(
            "CMSConnectionString is missing from configuration.");

    protected override string ObjectType => ContactTagsInfo.OBJECT_TYPE;

    public override Task ConfigurePage()
    {
        PageConfiguration.HeaderActions
            .AddLink<ContactTagsCreatePage>("New Contact Tag");

        PageConfiguration.ColumnConfigurations
            .AddColumn(
                nameof(ContactTagsInfo.ContactTagsID),
                "Item ID",
                visible: true,
                minWidth: 20,
                maxWidth: 25,
                searchable: false,
                localizable: false,
                sortable: true)

            .AddColumn(
                nameof(ContactTagsInfo.ItemCreatedBy),
                "Created by",
                visible: true,
                minWidth: 20,
                maxWidth: 25,
                searchable: false,
                localizable: false,
                sortable: true)

            .AddColumn(
                nameof(ContactTagsInfo.ItemCreatedWhen),
                "Created when",
                visible: true,
                minWidth: 20,
                maxWidth: 25,
                searchable: false,
                localizable: false,
                sortable: true)

            .AddColumn(
                nameof(ContactTagsInfo.ItemModifiedBy),
                "Modified by",
                visible: true,
                minWidth: 20,
                maxWidth: 25,
                searchable: false,
                localizable: false,
                sortable: true)

            .AddColumn(
                nameof(ContactTagsInfo.ItemModifiedWhen),
                "Modified when",
                visible: true,
                minWidth: 20,
                maxWidth: 25,
                searchable: false,
                localizable: false,
                sortable: true)

            .AddColumn(
                nameof(ContactTagsInfo.ItemOrder),
                "Item order",
                visible: true,
                minWidth: 20,
                maxWidth: 25        ,
                searchable: false,
                localizable: false,
                sortable: true)

            .AddColumn(
                nameof(ContactTagsInfo.ItemGUID),
                "Item GUID",
                visible: true,
                minWidth: 20,
                maxWidth: 25,
                searchable: false,
                localizable: false,
                sortable: false)

            .AddColumn(
                nameof(ContactTagsInfo.Name),
                "Name",
                visible: true,
                minWidth: 20,
                maxWidth: 25,
                searchable: true,
                localizable: false,
                sortable: true);

        PageConfiguration.AddEditRowAction<ContactTagsEditSection>();

        PageConfiguration.TableActions
            .AddDeleteAction(nameof(DeleteContactTag), "Delete");

        PageConfiguration.QueryModifiers.AddModifier(
            (query, _) => query.Columns(
                nameof(ContactTagsInfo.ContactTagsID),
                nameof(ContactTagsInfo.ItemCreatedBy),
                nameof(ContactTagsInfo.ItemCreatedWhen),
                nameof(ContactTagsInfo.ItemModifiedBy),
                nameof(ContactTagsInfo.ItemModifiedWhen),
                nameof(ContactTagsInfo.ItemOrder),
                nameof(ContactTagsInfo.ItemGUID),
                nameof(ContactTagsInfo.Name)));

        return base.ConfigurePage();
    }

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public async Task<ICommandResponse<RowActionResult>> DeleteContactTag(
     int id,
     CancellationToken cancellationToken)
    {
        var contactTag = await Provider<ContactTagsInfo>
            .Instance.GetAsync(id, cancellationToken);

        if (contactTag is null)
        {
            return ResponseFrom(new RowActionResult(reload: false))
                .AddErrorMessage("Contact Tag not found.");
        }

        try
        {
            await ResequenceAfterDeleteAsync(id, cancellationToken);

            CacheHelper.TouchKey(
                $"object|{ContactTagsInfo.OBJECT_TYPE}|all");

            return ResponseFrom(new RowActionResult(reload: true))
                .AddSuccessMessage("Contact Tag deleted successfully.");
        }
        catch (Exception)
        {
            return ResponseFrom(new RowActionResult(reload: false))
                .AddErrorMessage("Unable to delete Contact Tag.");
        }
    }

    private async Task ResequenceAfterDeleteAsync(
        int deletedId,
        CancellationToken cancellationToken)
    {
        const string selectSql = """
            SELECT
                ContactTagsID,
                ItemCreatedBy,
                ItemCreatedWhen,
                ItemModifiedBy,
                ItemModifiedWhen,
                ItemOrder,
                ItemGUID,
                Name
            FROM dbo.ETG_ContactTags WITH (TABLOCKX, HOLDLOCK)
            WHERE ContactTagsID <> @DeletedID
            ORDER BY ContactTagsID;
            """;

        var rows = new List<ContactTagRow>();

        await using var connection =
            new SqlConnection(connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var transaction =
            await connection.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            // Read all remaining records before rebuilding the table.
            await using (var selectCommand =
                new SqlCommand(
                    selectSql,
                    connection,
                    (SqlTransaction)transaction))
            {
                selectCommand.Parameters
                    .Add("@DeletedID", SqlDbType.Int)
                    .Value = deletedId;

                await using var reader =
                    await selectCommand.ExecuteReaderAsync(
                        cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    rows.Add(new ContactTagRow(
                        reader.GetInt32(0),
                        reader.GetInt32(1),
                        reader.GetDateTime(2),
                        reader.IsDBNull(3)
                            ? null
                            : reader.GetInt32(3),
                        reader.IsDBNull(4)
                            ? null
                            : reader.GetDateTime(4),
                        reader.GetInt32(5),
                        reader.GetGuid(6),
                        reader.IsDBNull(7)
                            ? string.Empty
                            : reader.GetString(7)));
                }
            }

            // Preserve the original first ID as the sequence starting point.
            int firstId;

            await using (var minCommand =
                new SqlCommand(
                    "SELECT MIN(ContactTagsID) FROM dbo.ETG_ContactTags WITH (TABLOCKX, HOLDLOCK);",
                    connection,
                    (SqlTransaction)transaction))
            {
                var value =
                    await minCommand.ExecuteScalarAsync(
                        cancellationToken);

                firstId = value == DBNull.Value
                    ? 1
                    : Convert.ToInt32(value);
            }

            // Delete all rows; they will be inserted again with sequential IDs.
            await using (var deleteCommand =
                new SqlCommand(
                    "DELETE FROM dbo.ETG_ContactTags;",
                    connection,
                    (SqlTransaction)transaction))
            {
                await deleteCommand.ExecuteNonQueryAsync(
                    cancellationToken);
            }

            if (rows.Count > 0)
            {
                // Identity column requires IDENTITY_INSERT for explicit IDs.
                await using (var identityOnCommand =
                    new SqlCommand(
                        "SET IDENTITY_INSERT dbo.ETG_ContactTags ON;",
                        connection,
                        (SqlTransaction)transaction))
                {
                    await identityOnCommand.ExecuteNonQueryAsync(
                        cancellationToken);
                }

                const string insertSql = """
                    INSERT INTO dbo.ETG_ContactTags
                    (
                        ContactTagsID,
                        ItemCreatedBy,
                        ItemCreatedWhen,
                        ItemModifiedBy,
                        ItemModifiedWhen,
                        ItemOrder,
                        ItemGUID,
                        Name
                    )
                    VALUES
                    (
                        @ContactTagsID,
                        @ItemCreatedBy,
                        @ItemCreatedWhen,
                        @ItemModifiedBy,
                        @ItemModifiedWhen,
                        @ItemOrder,
                        @ItemGUID,
                        @Name
                    );
                    """;

                for (int index = 0; index < rows.Count; index++)
                {
                    var row = rows[index];
                    int sequentialId = firstId + index;

                    await using var insertCommand =
                        new SqlCommand(
                            insertSql,
                            connection,
                            (SqlTransaction)transaction);

                    insertCommand.Parameters
                        .Add("@ContactTagsID", SqlDbType.Int)
                        .Value = sequentialId;

                    insertCommand.Parameters
                        .Add("@ItemCreatedBy", SqlDbType.Int)
                        .Value = row.CreatedBy;

                    insertCommand.Parameters
                        .Add("@ItemCreatedWhen", SqlDbType.DateTime2)
                        .Value = row.CreatedWhen;

                    insertCommand.Parameters
                        .Add("@ItemModifiedBy", SqlDbType.Int)
                        .Value = (object?)row.ModifiedBy
                            ?? DBNull.Value;

                    insertCommand.Parameters
                        .Add("@ItemModifiedWhen", SqlDbType.DateTime2)
                        .Value = (object?)row.ModifiedWhen
                            ?? DBNull.Value;

                    // ItemOrder is also kept sequential.
                    insertCommand.Parameters
                        .Add("@ItemOrder", SqlDbType.Int)
                        .Value = sequentialId;

                    insertCommand.Parameters
                        .Add("@ItemGUID", SqlDbType.UniqueIdentifier)
                        .Value = row.ItemGuid;

                    insertCommand.Parameters
                        .Add("@Name", SqlDbType.NVarChar, -1)
                        .Value = row.Name;

                    await insertCommand.ExecuteNonQueryAsync(
                        cancellationToken);
                }

                await using var identityOffCommand =
                    new SqlCommand(
                        "SET IDENTITY_INSERT dbo.ETG_ContactTags OFF;",
                        connection,
                        (SqlTransaction)transaction);

                await identityOffCommand.ExecuteNonQueryAsync(
                    cancellationToken);
            }
            else
            {
                // Empty table: next generated identity will be 1.
                await using var reseedCommand =
                    new SqlCommand(
                        "DBCC CHECKIDENT ('dbo.ETG_ContactTags', RESEED, 0);",
                        connection,
                        (SqlTransaction)transaction);

                await reseedCommand.ExecuteNonQueryAsync(
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            // Make sure IDENTITY_INSERT is switched off before rollback.
            try
            {
                await using var identityOffCommand =
                    new SqlCommand(
                        "SET IDENTITY_INSERT dbo.ETG_ContactTags OFF;",
                        connection,
                        (SqlTransaction)transaction);

                await identityOffCommand.ExecuteNonQueryAsync(
                    cancellationToken);
            }
            catch
            {
                // Preserve the original exception.
            }

            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private sealed record ContactTagRow(
        int OldId,
        int CreatedBy,
        DateTime CreatedWhen,
        int? ModifiedBy,
        DateTime? ModifiedWhen,
        int ItemOrder,
        Guid ItemGuid,
        string Name);
}

[UIEvaluatePermission(SystemPermissions.CREATE)]
public class ContactTagsCreatePage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator)
    : CreatePage<ContactTagsInfo, ContactTagsEditSection>(
        formComponentMapper,
        formDataBinder,
        pageLinkGenerator)
{
    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            ContactTagsPageSettings.FormName;

        return base.ConfigurePage();
    }

    protected override Task<ICommandResponse> GetSubmitSuccessResponse(
        ContactTagsInfo savedInfoObject,
        ICollection<IFormItem> items)
    {
        string listUrl =
            pageLinkGenerator.GetPath<ContactTagsListPage>();

        return Task.FromResult(
            (ICommandResponse)NavigateTo(listUrl));
    }
}

public class ContactTagsEditSection
    : EditSectionPage<ContactTagsInfo>
{
}

[UIEvaluatePermission(SystemPermissions.UPDATE)]
public class ContactTagsEditPage(
    IFormComponentMapper formComponentMapper,
    IFormDataBinder formDataBinder,
    IPageLinkGenerator pageLinkGenerator)
    : InfoEditPage<ContactTagsInfo>(
        formComponentMapper,
        formDataBinder)
{
    [PageParameter(
        typeof(IntPageModelBinder),
        typeof(ContactTagsEditSection))]
    public override int ObjectId { get; set; }

    public override Task ConfigurePage()
    {
        PageConfiguration.UIFormName =
            ContactTagsPageSettings.FormName;

        return base.ConfigurePage();
    }

    protected override Task<ICommandResponse> GetSubmitSuccessResponse(
        ContactTagsInfo savedInfoObject,
        ICollection<IFormItem> items)
    {
        string listUrl =
            pageLinkGenerator.GetPath<ContactTagsListPage>();

        return Task.FromResult(
            (ICommandResponse)NavigateTo(listUrl));
    }
}