using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using ETG.Module.Booking.Classes.Info;

[assembly: RegisterObjectType(typeof(PaymentInfo), PaymentInfo.OBJECT_TYPE)]

namespace ETG.Module.Booking.Classes.Info
{
    /// <summary>
    /// Data container class for <see cref="PaymentInfo"/>.
    /// </summary>
    public partial class PaymentInfo : AbstractInfo<PaymentInfo, IInfoProvider<PaymentInfo>>, IInfoWithId, IInfoWithName, IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "etg.payment";


        /// <summary>
        /// Type information.
        /// </summary>
#warning "You will need to configure the type info."
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<PaymentInfo>), OBJECT_TYPE, "ETG.Payment", "PaymentID", "PaymentLastModified", "PaymentGuid", null, null, null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Payment ID.
        /// </summary>
        [DatabaseField]
        public virtual int PaymentID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(PaymentID)), 0);
            set => SetValue(nameof(PaymentID), value);
        }


        /// <summary>
        /// Payment merchant unique payment id.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentMerchantUniquePaymentId
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentMerchantUniquePaymentId)), String.Empty);
            set => SetValue(nameof(PaymentMerchantUniquePaymentId), value, String.Empty);
        }


        /// <summary>
        /// Payment first name.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentFirstName
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentFirstName)), String.Empty);
            set => SetValue(nameof(PaymentFirstName), value);
        }


        /// <summary>
        /// Payment last name.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentLastName
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentLastName)), String.Empty);
            set => SetValue(nameof(PaymentLastName), value);
        }


        /// <summary>
        /// Payment email.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentEmail
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentEmail)), String.Empty);
            set => SetValue(nameof(PaymentEmail), value);
        }


        /// <summary>
        /// Payment contact number.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentContactNumber
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentContactNumber)), String.Empty);
            set => SetValue(nameof(PaymentContactNumber), value);
        }


        /// <summary>
        /// Payment invoice reference.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentInvoiceReference
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentInvoiceReference)), String.Empty);
            set => SetValue(nameof(PaymentInvoiceReference), value);
        }


        /// <summary>
        /// Payment amount.
        /// </summary>
        [DatabaseField]
        public virtual double PaymentAmount
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(PaymentAmount)), 0d);
            set => SetValue(nameof(PaymentAmount), value, 0d);
        }


        /// <summary>
        /// Payment amount paid.
        /// </summary>
        [DatabaseField]
        public virtual double PaymentAmountPaid
        {
            get => ValidationHelper.GetDouble(GetValue(nameof(PaymentAmountPaid)), 0d);
            set => SetValue(nameof(PaymentAmountPaid), value, 0d);
        }


        /// <summary>
        /// Payment reference.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentReference
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentReference)), String.Empty);
            set => SetValue(nameof(PaymentReference), value, String.Empty);
        }


        /// <summary>
        /// Payment additional info.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentAdditionalInfo
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentAdditionalInfo)), String.Empty);
            set => SetValue(nameof(PaymentAdditionalInfo), value, String.Empty);
        }


        /// <summary>
        /// Payment details.
        /// </summary>
        [DatabaseField]
        public virtual string PaymentDetails
        {
            get => ValidationHelper.GetString(GetValue(nameof(PaymentDetails)), String.Empty);
            set => SetValue(nameof(PaymentDetails), value, String.Empty);
        }


        /// <summary>
        /// Payment guid.
        /// </summary>
        [DatabaseField]
        public virtual Guid PaymentGuid
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(PaymentGuid)), Guid.Empty);
            set => SetValue(nameof(PaymentGuid), value);
        }


        /// <summary>
        /// Payment created.
        /// </summary>
        [DatabaseField]
        public virtual DateTime PaymentCreated
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(PaymentCreated)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(PaymentCreated), value, DateTimeHelper.ZERO_TIME);
        }


        /// <summary>
        /// Payment last modified.
        /// </summary>
        [DatabaseField]
        public virtual DateTime PaymentLastModified
        {
            get => ValidationHelper.GetDateTime(GetValue(nameof(PaymentLastModified)), DateTimeHelper.ZERO_TIME);
            set => SetValue(nameof(PaymentLastModified), value);
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
        /// Creates an empty instance of the <see cref="PaymentInfo"/> class.
        /// </summary>
        public PaymentInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="PaymentInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public PaymentInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}