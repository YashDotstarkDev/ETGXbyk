namespace ETG.Core.Content;

public class BasePage
{
    public string Title { get; }
    public string ShortDescription { get; }

    public BasePage(IBasicItemFields basicItem)
    {
        Title = basicItem.BasicItemTitle ?? string.Empty;
        ShortDescription = basicItem.BasicItemShortDescription ?? string.Empty;
    }

    public BasePage(string title, string shortDescription)
    {
        Title = title ?? string.Empty;
        ShortDescription = shortDescription ?? string.Empty;
    }
}