using System;
using System.Data;
using CMS.DataEngine;
using CMS.Helpers;

namespace ETG.Core.Modules;

public partial class ArticleCategoryInfo
    : AbstractInfo<ArticleCategoryInfo, IInfoProvider<ArticleCategoryInfo>>,
      IInfoWithId,
      IInfoWithName
{
    public const string OBJECT_TYPE = "etg.articlecategory";

    public static readonly ObjectTypeInfo TYPEINFO =
        new(
            typeof(IInfoProvider<ArticleCategoryInfo>),
            OBJECT_TYPE,
            "ETG.ArticleCategory",
            "ArticleCategoryID",
            "ArticleCategoryLastModified",
            "ArticleCategoryGuid",
            "ArticleCategoryCodeName",
            "ArticleCategoryName",
            null,
            null,
            null)
        {
            TouchCacheDependencies = true
        };

    [DatabaseField]
    public virtual int ArticleCategoryID
    {
        get => ValidationHelper.GetInteger(
            GetValue(nameof(ArticleCategoryID)), 0);
        set => SetValue(nameof(ArticleCategoryID), value);
    }

    [DatabaseField]
    public virtual string ArticleCategoryName
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(ArticleCategoryName)), string.Empty);
        set => SetValue(nameof(ArticleCategoryName), value);
    }

    [DatabaseField]
    public virtual string ArticleCategoryCodeName
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(ArticleCategoryCodeName)), string.Empty);
        set => SetValue(nameof(ArticleCategoryCodeName), value);
    }

    [DatabaseField]
    public virtual Guid ArticleCategoryGuid
    {
        get => ValidationHelper.GetGuid(
            GetValue(nameof(ArticleCategoryGuid)), Guid.Empty);
        set => SetValue(nameof(ArticleCategoryGuid), value);
    }

    [DatabaseField]
    public virtual DateTime ArticleCategoryLastModified
    {
        get => ValidationHelper.GetDateTime(
            GetValue(nameof(ArticleCategoryLastModified)),
            DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(ArticleCategoryLastModified), value);
    }

    public ArticleCategoryInfo() : base(TYPEINFO)
    {
    }

    public ArticleCategoryInfo(DataRow dataRow) : base(TYPEINFO, dataRow)
    {
    }

    protected override void DeleteObject() => Provider.Delete(this);

    protected override void SetObject() => Provider.Set(this);
}