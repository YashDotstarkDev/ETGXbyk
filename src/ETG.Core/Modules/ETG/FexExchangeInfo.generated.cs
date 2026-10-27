using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using ETG.Core.Modules;

[assembly: RegisterObjectType(typeof(FexExchangeInfo), FexExchangeInfo.OBJECT_TYPE)]

namespace ETG.Core.Modules
{
    /// <summary>
    /// Data container class for <see cref="FexExchangeInfo"/>.
    /// </summary>
    public partial class FexExchangeInfo : AbstractInfo<FexExchangeInfo, IInfoProvider<FexExchangeInfo>>, IInfoWithId
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "etg.fexexchange";


        /// <summary>
        /// Type information.
        /// </summary>
//#warning "You will need to configure the type info."
        public static readonly ObjectTypeInfo TYPEINFO = new ObjectTypeInfo(typeof(IInfoProvider<FexExchangeInfo>), OBJECT_TYPE, "ETG.FexExchange", "FexExchangeID", null, null, null, null, null, null, null)
        {
            TouchCacheDependencies = true,
        };


        /// <summary>
        /// Fex exchange ID.
        /// </summary>
        [DatabaseField]
        public virtual int FexExchangeID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(FexExchangeID)), 0);
            set => SetValue(nameof(FexExchangeID), value);
        }


        /// <summary>
        /// Source currency.
        /// </summary>
        [DatabaseField]
        public virtual string SourceCurrency
        {
            get => ValidationHelper.GetString(GetValue(nameof(SourceCurrency)), String.Empty);
            set => SetValue(nameof(SourceCurrency), value);
        }


        /// <summary>
        /// Target currency.
        /// </summary>
        [DatabaseField]
        public virtual string TargetCurrency
        {
            get => ValidationHelper.GetString(GetValue(nameof(TargetCurrency)), String.Empty);
            set => SetValue(nameof(TargetCurrency), value);
        }


        /// <summary>
        /// Target currency value.
        /// </summary>
        [DatabaseField]
        public virtual decimal TargetCurrencyValue
        {
            get => ValidationHelper.GetDecimal(GetValue(nameof(TargetCurrencyValue)), 0m);
            set => SetValue(nameof(TargetCurrencyValue), value);
        }


        /// <summary>
        /// Adjusted target value.
        /// </summary>
        [DatabaseField]
        public virtual decimal AdjustedTargetValue
        {
            get => ValidationHelper.GetDecimal(GetValue(nameof(AdjustedTargetValue)), 0m);
            set => SetValue(nameof(AdjustedTargetValue), value);
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
        /// Creates an empty instance of the <see cref="FexExchangeInfo"/> class.
        /// </summary>
        public FexExchangeInfo()
            : base(TYPEINFO)
        {
        }


        /// <summary>
        /// Creates a new instances of the <see cref="FexExchangeInfo"/> class from the given <see cref="DataRow"/>.
        /// </summary>
        /// <param name="dr">DataRow with the object data.</param>
        public FexExchangeInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}