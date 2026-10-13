using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using ETGBooking;

[assembly: RegisterObjectType(typeof(PromotionInfo), PromotionInfo.OBJECT_TYPE)]

namespace ETGBooking
{
    /// <summary>
    /// Data container class for <see cref="PromotionInfo"/>.
    /// </summary>
    public partial class PromotionInfo : AbstractInfo<PromotionInfo, IInfoProvider<PromotionInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "etg.promotion";


        /// <summary>
        /// Type information.
        /// </summary>
#warning "You will need to configure the type info."
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<PromotionInfo>), OBJECT_TYPE, "ETG.Promotion", "PromotionID", "PromotionLastModified", "PromotionGuid", "PromotionPromoCode", "PromotionName", null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Promotion ID.
        /// </summary>
        [DatabaseField]
        public virtual int PromotionID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(PromotionID)), 0);
            set => SetValue(nameof(PromotionID), value);
        }


        /// <summary>
        /// Promotion name.
        /// </summary>
        [DatabaseField]
        public virtual string PromotionName
        {
            get => ValidationHelper.GetString(GetValue(nameof(PromotionName)), String.Empty);
            set => SetValue(nameof(PromotionName), value);
        }


        /// <summary>
        /// Promotion promo code.
        /// </summary>
        [DatabaseField]
        public virtual string PromotionPromoCode
        {
            get => ValidationHelper.GetString(GetValue(nameof(PromotionPromoCode)), String.Empty);
            set => SetValue(nameof(PromotionPromoCode), value, String.Empty);
        }


        /// <summary>
        /// Promotion destinations.
        /// </summary>
        [DatabaseField]
        public virtual string PromotionDestinations
        {
            get => ValidationHelper.GetString(GetValue(nameof(PromotionDestinations)), String.Empty);
            set => SetValue(nameof(PromotionDestinations), value, String.Empty);
        }


        /// <summary>
        /// Promotion email domains.
        /// </summary>
        [DatabaseField]
        public virtual string PromotionEmailDomains
        {
            get => ValidationHelper.GetString(GetValue(nameof(PromotionEmailDomains)), String.Empty);
            set => SetValue(nameof(PromotionEmailDomains), value, String.Empty);
        }


        /// <summary>
        /// Promotion from date.
        /// </summary>
        [DatabaseField]
        public virtual DateTime PromotionFromDate
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(PromotionFromDate)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(PromotionFromDate), value);
        }


        /// <summary>
        /// Promotion to date.
        /// </summary>
        [DatabaseField]
        public virtual DateTime PromotionToDate
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(PromotionToDate)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(PromotionToDate), value);
        }


        /// <summary>
        /// Promotion percent discount.
        /// </summary>
        [DatabaseField]
        public virtual double PromotionPercentDiscount
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(PromotionPercentDiscount)), 0d);
            set => SetValue(nameof(PromotionPercentDiscount), value, 0d);
        }


        /// <summary>
        /// Promotion dollar discount.
        /// </summary>
        [DatabaseField]
        public virtual int PromotionDollarDiscount
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(PromotionDollarDiscount)), 0);
            set => SetValue(nameof(PromotionDollarDiscount), value, 0);
        }


        /// <summary>
        /// Promotion package codes.
        /// </summary>
        [DatabaseField]
        public virtual string PromotionPackageCodes
        {
            get => ValidationHelper.GetString(GetValue(nameof(PromotionPackageCodes)), String.Empty);
            set => SetValue(nameof(PromotionPackageCodes), value, String.Empty);
        }


        /// <summary>
        /// Promotion package dates.
        /// </summary>
        [DatabaseField]
        public virtual string PromotionPackageDates
        {
            get => ValidationHelper.GetString(GetValue(nameof(PromotionPackageDates)), String.Empty);
            set => SetValue(nameof(PromotionPackageDates), value, String.Empty);
        }


        /// <summary>
        /// Promotion guid.
        /// </summary>
        [DatabaseField]
        public virtual Guid PromotionGuid
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(PromotionGuid)), Guid.Empty);
            set => SetValue(nameof(PromotionGuid), value);
        }


        /// <summary>
        /// Promotion last modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime PromotionLastModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(PromotionLastModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(PromotionLastModified), value);
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
        /// Creates an empty instance of the <see cref="PromotionInfo"/> class.
        /// </summary>
        public PromotionInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="PromotionInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public PromotionInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}