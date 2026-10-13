using System;
using System.Data;
using CMS.DataEngine;
using CMS.Helpers;

namespace ETG.Core.Modules;

public partial class TourTypeInfo
    : AbstractInfo<TourTypeInfo, IInfoProvider<TourTypeInfo>>,
      IInfoWithId,
      IInfoWithName
{
    public const string OBJECT_TYPE = "etg.tourtype";

    public static readonly ObjectTypeInfo TYPEINFO =
        new(
            typeof(IInfoProvider<TourTypeInfo>),
            OBJECT_TYPE,
            "ETG.TourType",
            "TourTypeID",
            "TourTypeLastModified",
            "TourTypeGuid",
            "TourTypeCodeName",
            "TourTypeName",
            null,
            null,
            null)
        {
            TouchCacheDependencies = true
        };

    [DatabaseField]
    public virtual int TourTypeID
    {
        get => ValidationHelper.GetInteger(
            GetValue(nameof(TourTypeID)), 0);
        set => SetValue(nameof(TourTypeID), value);
    }

    [DatabaseField]
    public virtual string TourTypeName
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(TourTypeName)), string.Empty);
        set => SetValue(nameof(TourTypeName), value);
    }

    [DatabaseField]
    public virtual string TourTypeCodeName
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(TourTypeCodeName)), string.Empty);
        set => SetValue(nameof(TourTypeCodeName), value);
    }

    [DatabaseField]
    public virtual string TourTypeIcon
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(TourTypeIcon)), string.Empty);
        set => SetValue(nameof(TourTypeIcon), value);
    }

    [DatabaseField]
    public virtual string TourTypeWhiteIconImage
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(TourTypeWhiteIconImage)), string.Empty);
        set => SetValue(nameof(TourTypeWhiteIconImage), value);
    }

    [DatabaseField]
    public virtual string TourTypeBlackIconImage
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(TourTypeBlackIconImage)), string.Empty);
        set => SetValue(nameof(TourTypeBlackIconImage), value);
    }

    [DatabaseField]
    public virtual string TourTypeDescription
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(TourTypeDescription)), string.Empty);
        set => SetValue(nameof(TourTypeDescription), value);
    }

    [DatabaseField]
    public virtual string TourTypeUrl
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(TourTypeUrl)), string.Empty);
        set => SetValue(nameof(TourTypeUrl), value);
    }

    [DatabaseField]
    public virtual Guid TourTypeGuid
    {
        get => ValidationHelper.GetGuid(
            GetValue(nameof(TourTypeGuid)), Guid.Empty);
        set => SetValue(nameof(TourTypeGuid), value);
    }

    [DatabaseField]
    public virtual DateTime TourTypeLastModified
    {
        get => ValidationHelper.GetDateTime(
            GetValue(nameof(TourTypeLastModified)),
            DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(TourTypeLastModified), value);
    }

    public TourTypeInfo() : base(TYPEINFO)
    {
    }

    public TourTypeInfo(DataRow dataRow) : base(TYPEINFO, dataRow)
    {
    }

    protected override void DeleteObject() => Provider.Delete(this);

    protected override void SetObject() => Provider.Set(this);
}