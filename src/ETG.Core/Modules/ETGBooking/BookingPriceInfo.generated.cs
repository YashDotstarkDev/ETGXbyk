using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using ETG.Booking.Pricing.Classes.Info;

[assembly: RegisterObjectType(typeof(BookingPriceInfo), BookingPriceInfo.OBJECT_TYPE)]

namespace ETG.Booking.Pricing.Classes.Info
{
    /// <summary>
    /// Data container class for <see cref="BookingPriceInfo"/>.
    /// </summary>
    public partial class BookingPriceInfo : AbstractInfo<BookingPriceInfo, IInfoProvider<BookingPriceInfo>>, IInfoWithId, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "etg.bookingprice";


        /// <summary>
        /// Type information.
        /// </summary>
#warning "You will need to configure the type info."
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<BookingPriceInfo>), OBJECT_TYPE, "ETG.BookingPrice", "BookingPriceID", "BookingPriceLastModified", "BookingPriceGuid", null, nameof(BookingPriceTourCode), null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Booking price ID.
        /// </summary>
        [DatabaseField]
        public virtual int BookingPriceID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(BookingPriceID)), 0);
            set => SetValue(nameof(BookingPriceID), value);
        }


        /// <summary>
        /// Booking price tour code.
        /// </summary>
        [DatabaseField]
        public virtual string BookingPriceTourCode
        {
            get => ValidationHelper.GetString(GetValue(nameof(BookingPriceTourCode)), String.Empty);
            set => SetValue(nameof(BookingPriceTourCode), value);
        }


        /// <summary>
        /// Booking price start date.
        /// </summary>
        [DatabaseField]
        public virtual DateTime BookingPriceStartDate
        {
            get => ValidationHelper.GetDate(GetValue(nameof(BookingPriceStartDate)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(BookingPriceStartDate), value);
        }


        /// <summary>
        /// Booking price end date.
        /// </summary>
        [DatabaseField]
        public virtual DateTime BookingPriceEndDate
        {
            get => ValidationHelper.GetDate(GetValue(nameof(BookingPriceEndDate)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(BookingPriceEndDate), value);
        }


        /// <summary>
        /// Booking price twin share price.
        /// </summary>
        [DatabaseField]
        public virtual double BookingPriceTwinSharePrice
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(BookingPriceTwinSharePrice)), 0d);
            set => SetValue(nameof(BookingPriceTwinSharePrice), value, 0d);
        }


        /// <summary>
        /// Booking price single supplemental cost.
        /// </summary>
        [DatabaseField]
        public virtual double BookingPriceSingleSupplementalCost
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(BookingPriceSingleSupplementalCost)), 0d);
            set => SetValue(nameof(BookingPriceSingleSupplementalCost), value, 0d);
        }


        /// <summary>
        /// Booking price pre night twin price.
        /// </summary>
        [DatabaseField]
        public virtual double BookingPricePreNightTwinPrice
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(BookingPricePreNightTwinPrice)), 0d);
            set => SetValue(nameof(BookingPricePreNightTwinPrice), value, 0d);
        }


        /// <summary>
        /// Booking price post night twin price.
        /// </summary>
        [DatabaseField]
        public virtual double BookingPricePostNightTwinPrice
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(BookingPricePostNightTwinPrice)), 0d);
            set => SetValue(nameof(BookingPricePostNightTwinPrice), value, 0d);
        }


        /// <summary>
        /// Booking price pre night single price.
        /// </summary>
        [DatabaseField]
        public virtual double BookingPricePreNightSinglePrice
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(BookingPricePreNightSinglePrice)), 0d);
            set => SetValue(nameof(BookingPricePreNightSinglePrice), value, 0d);
        }


        /// <summary>
        /// Booking price post night single price.
        /// </summary>
        [DatabaseField]
        public virtual double BookingPricePostNightSinglePrice
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(BookingPricePostNightSinglePrice)), 0d);
            set => SetValue(nameof(BookingPricePostNightSinglePrice), value, 0d);
        }


        /// <summary>
        /// Balance due days.
        /// </summary>
        [DatabaseField]
        public virtual int BalanceDueDays
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(BalanceDueDays)), 0);
            set => SetValue(nameof(BalanceDueDays), value, 0);
        }


        /// <summary>
        /// Second instalment days.
        /// </summary>
        [DatabaseField]
        public virtual int SecondInstalmentDays
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(SecondInstalmentDays)), 0);
            set => SetValue(nameof(SecondInstalmentDays), value, 0);
        }


        /// <summary>
        /// Second instalment percent.
        /// </summary>
        [DatabaseField]
        public virtual double SecondInstalmentPercent
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(SecondInstalmentPercent)), 0d);
            set => SetValue(nameof(SecondInstalmentPercent), value, 0d);
        }


        /// <summary>
        /// Is enabled.
        /// </summary>
        [DatabaseField]
        public virtual bool IsEnabled
        {
            get => ValidationHelper.GetBoolean(GetValue(nameof(IsEnabled)), true);
            set => SetValue(nameof(IsEnabled), value);
        }


        /// <summary>
        /// Booking price guid.
        /// </summary>
        [DatabaseField]
        public virtual Guid BookingPriceGuid
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(BookingPriceGuid)), Guid.Empty);
            set => SetValue(nameof(BookingPriceGuid), value);
        }


        /// <summary>
        /// Booking price last modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime BookingPriceLastModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(BookingPriceLastModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(BookingPriceLastModified), value);
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
        /// Creates an empty instance of the <see cref="BookingPriceInfo"/> class.
        /// </summary>
        public BookingPriceInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="BookingPriceInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public BookingPriceInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}