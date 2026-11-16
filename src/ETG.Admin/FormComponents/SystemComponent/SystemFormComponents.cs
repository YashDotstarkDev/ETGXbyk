// File location: src/ETG.Admin/FormComponents/SystemComponent/SystemFormComponents.cs

using System;
using System.Threading.Tasks;
using CMS;
using CMS.DataEngine;
using ETG.Web.FormComponents;
using Kentico.Forms.Web.Mvc;
using Kentico.Web.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.Extensions.DependencyInjection;

// Tells ASP.NET Core MVC to load compiled Razor views (RazorCompiledItem) from ETG.Admin.dll
[assembly: ProvideApplicationPartFactory(typeof(CompiledRazorAssemblyApplicationPartFactory))]

[assembly: RegisterFormComponent(
    SystemTimestampComponent.IDENTIFIER,
    typeof(SystemTimestampComponent),
    "Date and Time",
    IsAvailableInFormBuilderEditor = false,
    ViewName = "~/Views/Shared/FormComponents/_SystemTimestamp.cshtml")]

[assembly: RegisterFormComponent(
    GuidValueComponent.IDENTIFIER,
    typeof(GuidValueComponent),
    "GUID value",
    IsAvailableInFormBuilderEditor = false,
    ViewName = "~/Views/Shared/FormComponents/_GuidValue.cshtml")]

[assembly: RazorCompiledItem(
    typeof(SystemTimestampCompiledView),
    "mvc.1.0.view",
    "/Views/Shared/FormComponents/_SystemTimestamp.cshtml")]

[assembly: RazorCompiledItem(
    typeof(GuidValueCompiledView),
    "mvc.1.0.view",
    "/Views/Shared/FormComponents/_GuidValue.cshtml")]

namespace ETG.Web.FormComponents
{
    public sealed class SystemTimestampProperties : FormComponentProperties<DateTime?>
    {
        public SystemTimestampProperties() : base(FieldDataType.DateTime) { }
        public override DateTime? DefaultValue { get; set; }
    }

    public sealed class SystemTimestampComponent : FormComponent<SystemTimestampProperties, DateTime?>
    {
        public const string IDENTIFIER = "ETG.SystemTimestamp";

        [BindableProperty]
        public DateTime? Value { get; set; }

        public override DateTime? GetValue() => Value;
        public override void SetValue(DateTime? value) => Value = value;
    }

    [RazorCompiledItemMetadata("Identifier", "/Views/Shared/FormComponents/_SystemTimestamp.cshtml")]
    public sealed class SystemTimestampCompiledView : RazorPage<SystemTimestampComponent>
    {
        public override Task ExecuteAsync()
        {
            Layout = null;
            var html = ViewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper<SystemTimestampComponent>>();
            if (html is IViewContextAware contextualHelper) contextualHelper.Contextualize(ViewContext);

            var attributes = ViewData.Kentico().GetEditorHtmlAttributes();
            attributes["readonly"] = "readonly";

            Write(html.TextBoxFor(model => model.Value, "{0:yyyy-MM-dd HH:mm:ss}", attributes));
            return Task.CompletedTask;
        }
    }

    public sealed class GuidValueProperties : FormComponentProperties<Guid?>
    {
        public GuidValueProperties() : base(FieldDataType.Guid) { }
        public override Guid? DefaultValue { get; set; }
    }

    public sealed class GuidValueComponent : FormComponent<GuidValueProperties, Guid?>
    {
        public const string IDENTIFIER = "ETG.GuidValue";

        [BindableProperty]
        public Guid? Value { get; set; }

        public override Guid? GetValue() => Value;
        public override void SetValue(Guid? value) => Value = value;
    }

    [RazorCompiledItemMetadata("Identifier", "/Views/Shared/FormComponents/_GuidValue.cshtml")]
    public sealed class GuidValueCompiledView : RazorPage<GuidValueComponent>
    {
        public override Task ExecuteAsync()
        {
            Layout = null;
            var html = ViewContext.HttpContext.RequestServices.GetRequiredService<IHtmlHelper<GuidValueComponent>>();
            if (html is IViewContextAware contextualHelper) contextualHelper.Contextualize(ViewContext);

            var attributes = ViewData.Kentico().GetEditorHtmlAttributes();
            attributes["readonly"] = "readonly";

            Write(html.TextBoxFor(model => model.Value, attributes));
            return Task.CompletedTask;
        }
    }
}