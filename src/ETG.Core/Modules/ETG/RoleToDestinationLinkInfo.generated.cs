using System;
using System.Data;
using CMS.DataEngine;
using CMS.Helpers;

namespace ETG.Core.Modules;

public partial class RoleToDestinationLinkInfo
    : AbstractInfo<
        RoleToDestinationLinkInfo,
        IInfoProvider<RoleToDestinationLinkInfo>>,
      IInfoWithId,
      IInfoWithName
{
    public const string OBJECT_TYPE = "etg.roledestinationlink";

    public static readonly ObjectTypeInfo TYPEINFO =
        new(
            typeof(IInfoProvider<RoleToDestinationLinkInfo>),
            OBJECT_TYPE,
            "ETG.RoleDestinationLink",
            "RoleDestinationLinkID",
            "RoleDestinationLinkLastModified",
            "RoleDestinationLinkGuid",
            "RoleDestinationName",
            "RoleDestinationName",
            null,
            null,
            null)
        {
            TouchCacheDependencies = true
        };

    [DatabaseField]
    public virtual int RoleDestinationLinkID
    {
        get => ValidationHelper.GetInteger(
            GetValue(nameof(RoleDestinationLinkID)), 0);
        set => SetValue(nameof(RoleDestinationLinkID), value);
    }

    [DatabaseField]
    public virtual string RoleDestinationName
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(RoleDestinationName)), string.Empty);
        set => SetValue(nameof(RoleDestinationName), value);
    }

    [DatabaseField]
    public virtual Guid RoleDestinationRoleGuid
    {
        get => ValidationHelper.GetGuid(
            GetValue(nameof(RoleDestinationRoleGuid)), Guid.Empty);
        set => SetValue(nameof(RoleDestinationRoleGuid), value);
    }

    [DatabaseField]
    public virtual string RoleDestinationDestinations
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(RoleDestinationDestinations)), string.Empty);
        set => SetValue(nameof(RoleDestinationDestinations), value);
    }

    [DatabaseField]
    public virtual Guid RoleDestinationLinkGuid
    {
        get => ValidationHelper.GetGuid(
            GetValue(nameof(RoleDestinationLinkGuid)), Guid.Empty);
        set => SetValue(nameof(RoleDestinationLinkGuid), value);
    }

    [DatabaseField]
    public virtual DateTime RoleDestinationLinkLastModified
    {
        get => ValidationHelper.GetDateTime(
            GetValue(nameof(RoleDestinationLinkLastModified)),
            DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(RoleDestinationLinkLastModified), value);
    }

    public RoleToDestinationLinkInfo() : base(TYPEINFO)
    {
    }

    public RoleToDestinationLinkInfo(DataRow dataRow)
        : base(TYPEINFO, dataRow)
    {
    }

    protected override void DeleteObject() => Provider.Delete(this);

    protected override void SetObject() => Provider.Set(this);
}