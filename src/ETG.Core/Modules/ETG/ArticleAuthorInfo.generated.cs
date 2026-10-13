using System;
using System.Data;
using CMS.DataEngine;
using CMS.Helpers;

namespace ETG.Core.Modules;

public partial class ArticleAuthorInfo
    : AbstractInfo<ArticleAuthorInfo, IInfoProvider<ArticleAuthorInfo>>,
      IInfoWithId,
      IInfoWithName
{
    public const string OBJECT_TYPE = "etg.articleauthor";

    public static readonly ObjectTypeInfo TYPEINFO =
        new(
            typeof(IInfoProvider<ArticleAuthorInfo>),
            OBJECT_TYPE,
            "ETG.ArticleAuthor",
            "ArticleAuthorID",
            "ArticleAuthorLastModified",
            "ArticleAuthorGuid",
            "ArticleAuthorCodeName",
            "ArticleAuthorFullName",
            null,
            null,
            null)
        {
            TouchCacheDependencies = true
        };

    [DatabaseField]
    public virtual int ArticleAuthorID
    {
        get => ValidationHelper.GetInteger(
            GetValue(nameof(ArticleAuthorID)), 0);
        set => SetValue(nameof(ArticleAuthorID), value);
    }

    [DatabaseField]
    public virtual string ArticleAuthorFullName
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(ArticleAuthorFullName)), string.Empty);
        set => SetValue(nameof(ArticleAuthorFullName), value);
    }

    [DatabaseField]
    public virtual string ArticleAuthorCodeName
    {
        get => ValidationHelper.GetString(
            GetValue(nameof(ArticleAuthorCodeName)), string.Empty);
        set => SetValue(nameof(ArticleAuthorCodeName), value);
    }

    [DatabaseField]
    public virtual Guid ArticleAuthorGuid
    {
        get => ValidationHelper.GetGuid(
            GetValue(nameof(ArticleAuthorGuid)), Guid.Empty);
        set => SetValue(nameof(ArticleAuthorGuid), value);
    }

    [DatabaseField]
    public virtual DateTime ArticleAuthorLastModified
    {
        get => ValidationHelper.GetDateTime(
            GetValue(nameof(ArticleAuthorLastModified)),
            DateTimeHelper.ZERO_TIME);
        set => SetValue(nameof(ArticleAuthorLastModified), value);
    }

    public ArticleAuthorInfo() : base(TYPEINFO)
    {
    }

    public ArticleAuthorInfo(DataRow dataRow) : base(TYPEINFO, dataRow)
    {
    }

    protected override void DeleteObject() => Provider.Delete(this);

    protected override void SetObject() => Provider.Set(this);
}