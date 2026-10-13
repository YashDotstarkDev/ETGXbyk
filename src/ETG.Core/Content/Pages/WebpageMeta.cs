namespace ETG.Core.Content;

public record WebPageMeta
{
    public static WebPageMeta Default { get; } = new("", "", "");

    public WebPageMeta(IMetaDataSchema meta)
    {
        Title = Maybe
            .From(meta.MetaDataSchema__PageTitle)
            .MapNullOrWhiteSpaceAsNone()
            .IfNoValue(
                meta is IBasicItemFields basicItem
                    ? basicItem.BasicItemTitle
                    : Maybe<string>.None)
            .GetValueOrDefault("");

        Description = Maybe
            .From(meta.MetaDataSchema__PageDescription)
            .MapNullOrWhiteSpaceAsNone()
            .IfNoValue(
                meta is IBasicItemFields basicItem2
                    ? basicItem2.BasicItemShortDescription
                    : Maybe<string>.None)
            .GetValueOrDefault("");

        Keywords = meta.MetaDataSchema__PageKeywords;
    }

    public WebPageMeta(
        string title,
        string description,
        string keywords)
    {
        Title = title;
        Description = description;
        Keywords = keywords;
    }

    public string Title { get; init; }
    public string Description { get; init; }
    public string Keywords { get; init; } = "";

    // Added without changing existing constructor behavior.
    public string ShareImage { get; init; } = "";
}