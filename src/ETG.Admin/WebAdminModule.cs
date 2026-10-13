using CMS;
using CMS.Core;
using CMS.DataEngine;
using CMS.FormEngine;
using ETG.Admin;
using Kentico.Xperience.Admin.Base;

[assembly: RegisterModule(typeof(WebAdminModule))]

[assembly: UICategory(
   codeName: WebAdminModule.WEBSITE_CATEGORY,
   name: "Global",
   icon: Icons.PersonalisationVariants,
   order: 100)]

namespace ETG.Admin;

internal class WebAdminModule : AdminModule, IDataTypeRegister
{
    public const string WEBSITE_CATEGORY = "website.web-admin";

    public WebAdminModule() : base(nameof(WebAdminModule)) { }

    protected override void OnPreInit(ModulePreInitParameters parameters)
    {
        base.OnPreInit(parameters);
    }

    protected override void OnInit(ModuleInitParameters parameters)
    {
        base.OnInit(parameters);

        RegisterClientModule("website", "web-admin");
    }

    public void AddRegistrations(DataTypeRegistration[] registrations)
    {
        foreach (var registration in registrations)
        {
            DataTypeManager.RegisterDataTypes([registration.DataType]);
            if (registration.DataTypeCodeGenerator is { } generator)
            {
                DataTypeCodeGenerationManager.RegisterDataTypeCodeGenerator(generator.DataType, generator.GeneratorInitializer);
            }
            if (registration.DataType.HasConfigurableDefaultValue && registration.DefaultComponent is { } component)
            {
                RegisterDefaultValueComponent(component.DataType, component.ComponentIdentifier, component.SerializationFunction, component.DeserializationFunction);
            }
        }
    }
}

public interface IDataTypeRegister
{
    public void AddRegistrations(DataTypeRegistration[] registrations);
}

public record DataTypeRegistration(
    DataType DataType,
    DataTypeCodeGeneratorRegistration? DataTypeCodeGenerator,
    DataTypeDefaultComponentRegistration? DefaultComponent)
{
    public DataTypeRegistration(DataType dataType) : this(dataType, null, null) { }
    public DataTypeRegistration(DataType dataType, DataTypeCodeGeneratorRegistration? dataTypeCodeGenerator) : this(dataType, dataTypeCodeGenerator, null) { }
}

public record DataTypeDefaultComponentRegistration(
    string DataType,
    string ComponentIdentifier,
    Func<object, string> SerializationFunction,
    Func<string, object> DeserializationFunction);

public record DataTypeCodeGeneratorRegistration(
    string DataType,
    Func<DataTypeCodeGenerator> GeneratorInitializer);
