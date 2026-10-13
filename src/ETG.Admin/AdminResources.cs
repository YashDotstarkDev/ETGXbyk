using CMS.Base;
using CMS.Localization;
using ETG.Admin;

[assembly: RegisterLocalizationResource(
    typeof(AdminResources),
    SystemContext.SYSTEM_CULTURE_NAME)]

namespace ETG.Admin;

internal class AdminResources { }
