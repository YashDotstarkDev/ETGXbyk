using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using ETG.Core.Modules;

[assembly: RegisterObjectType(typeof(WebsiteSettingsInfo), WebsiteSettingsInfo.OBJECT_TYPE)]

namespace ETG.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="WebsiteSettingsInfo"/>.
    /// </summary>
    public partial class WebsiteSettingsInfo : AbstractInfo<WebsiteSettingsInfo, IInfoProvider<WebsiteSettingsInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "website.websitesettings";


        /// <summary>
        /// Type information.
        /// </summary>
#warning "You will need to configure the type info."
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<WebsiteSettingsInfo>), OBJECT_TYPE, "Website.WebsiteSettings", "WebsiteSettingsID", "WebsiteSettingsDateModified", "WebsiteSettingsGUID", "WebsiteSettingsName", "WebsiteSettingsName", null, null, null)
        {
            TouchCacheDependencies = true,
            DependsOn = new List<ObjectDependency>()
            {
            },
        };


        /// <summary>
        /// Website settings ID.
        /// </summary>
        [DatabaseField]
        public virtual int WebsiteSettingsID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(WebsiteSettingsID)), 0);
            set => SetValue(nameof(WebsiteSettingsID), value);
        }


        /// <summary>
        /// Website settings name.
        /// </summary>
        [DatabaseField]
        public virtual string WebsiteSettingsName
        {
            get => ValidationHelper.GetString(GetValue(nameof(WebsiteSettingsName)), String.Empty);
            set => SetValue(nameof(WebsiteSettingsName), value);
        }


        /// <summary>
        /// Website settings GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid WebsiteSettingsGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(WebsiteSettingsGUID)), Guid.Empty);
            set => SetValue(nameof(WebsiteSettingsGUID), value);
        }


        /// <summary>
        /// Website settings website global content.
        /// </summary>
        [DatabaseField(ValueType = typeof(string))]
        public IEnumerable<global::CMS.ContentEngine.ContentItemReference> WebsiteSettingsWebsiteGlobalContent
        {
            get => global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToModels<global::CMS.ContentEngine.ContentItemReference>(GetValue(nameof(WebsiteSettingsWebsiteGlobalContent), String.Empty));
            set => SetValue(nameof(WebsiteSettingsWebsiteGlobalContent), global::CMS.DataEngine.Internal.JsonDataTypeConverter.ConvertToString<IEnumerable<global::CMS.ContentEngine.ContentItemReference>>(value, Enumerable.Empty<global::CMS.ContentEngine.ContentItemReference>(), null));
        }


        /// <summary>
        /// Website settings date created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime WebsiteSettingsDateCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(WebsiteSettingsDateCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(WebsiteSettingsDateCreated), value);
        }


        /// <summary>
        /// Website settings date modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime WebsiteSettingsDateModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(WebsiteSettingsDateModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(WebsiteSettingsDateModified), value);
        }


        /// <summary>
        /// Google Place ID used by the Wextractor API.
        /// </summary>
        [DatabaseField]
        [Kentico.Xperience.Admin.Base.FormAnnotations.TextInputComponent(
            Label = "Google Place ID",
            ExplanationText = "The Google Place ID used to fetch reviews via the Wextractor API.",
            Order = 10)]
        public virtual string WebsiteSettingsGooglePlaceId
        {
            get => ValidationHelper.GetString(GetValue(nameof(WebsiteSettingsGooglePlaceId)), String.Empty);
            set => SetValue(nameof(WebsiteSettingsGooglePlaceId), value);
        }


        /// <summary>
        /// Wextractor API auth token.
        /// </summary>
        [DatabaseField]
        [Kentico.Xperience.Admin.Base.FormAnnotations.TextInputComponent(
            Label = "Wextractor Auth Token",
            ExplanationText = "The authentication token for the Wextractor Google Reviews API.",
            Order = 11)]
        public virtual string WebsiteSettingsWextractorAuthToken
        {
            get => ValidationHelper.GetString(GetValue(nameof(WebsiteSettingsWextractorAuthToken)), String.Empty);
            set => SetValue(nameof(WebsiteSettingsWextractorAuthToken), value);
        }


        /// <summary>
        /// Deletes the object using appropriate provider.
        /// </summary>
        protected override void DeleteObject()
        {
            Provider.Delete(this);
        }


        /// <summary>
        /// Updates the object using appropriate provider.
        /// </summary>
        protected override void SetObject()
        {
            Provider.Set(this);
        }


        /// <summary>
        /// Creates an empty instance of the <see cref="WebsiteSettingsInfo"/> class.
        /// </summary>
        public WebsiteSettingsInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="WebsiteSettingsInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public WebsiteSettingsInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}