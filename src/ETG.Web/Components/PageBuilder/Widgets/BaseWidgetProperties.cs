using Kentico.PageBuilder.Web.Mvc;

namespace ETG.Web.Components.PageBuilder.Widgets;

public abstract class BaseWidgetProperties : IWidgetProperties
{
    public Guid ID { get; set; } = Guid.NewGuid();
}