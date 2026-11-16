using System;
using System.Data;

using CMS;
using CMS.DataEngine;
using CMS.Helpers;
using CMS.Membership;

[assembly: RegisterObjectType(typeof(ETG.ContactTags.ContactTagsInfo), ETG.ContactTags.ContactTagsInfo.OBJECT_TYPE)]

namespace ETG.ContactTags
{
    /// <summary>
    /// Data container class for ContactTagsInfo.
    /// </summary>
    public partial class ContactTagsInfo :
        AbstractInfo<ContactTagsInfo, IInfoProvider<ContactTagsInfo>>,
        IInfoWithId,
        IInfoWithName,
        IInfoWithGuid
    {
        /// <summary>
        /// Object type.
        /// </summary>
        public const string OBJECT_TYPE = "etg.contacttags";

        /// <summary>
        /// Type information.
        /// </summary>
        public static readonly ObjectTypeInfo TYPEINFO =
            new ObjectTypeInfo(
                typeof(IInfoProvider<ContactTagsInfo>),
                OBJECT_TYPE,
                "ETG.ContactTags",
                "ContactTagsID",
                null,
                "ItemGUID",
                "Name",
                null,
                null,
                null,
                null)
            {
                TouchCacheDependencies = true
            };

        /// <summary>
        /// Contact tags ID.
        /// </summary>
        [DatabaseField]
        public virtual int ContactTagsID
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(ContactTagsID)), 0);
            set => SetValue(nameof(ContactTagsID), value);
        }

        /// <summary>
        /// Item created by.
        /// </summary>
        [DatabaseField]
        public virtual int ItemCreatedBy
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(ItemCreatedBy)), 0);
            set => SetValue(nameof(ItemCreatedBy), value, 0);
        }

        /// <summary>
        /// Item created when.
        /// </summary>
        [DatabaseField]
        public virtual DateTime ItemCreatedWhen
        {
            get => ValidationHelper.GetDateTime(
                GetValue(nameof(ItemCreatedWhen)),
                DateTimeHelper.ZERO_TIME);
            set => SetValue(
                nameof(ItemCreatedWhen),
                value,
                DateTimeHelper.ZERO_TIME);
        }

        /// <summary>
        /// Item modified by.
        /// </summary>
        [DatabaseField]
        public virtual int ItemModifiedBy
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(ItemModifiedBy)), 0);
            set => SetValue(nameof(ItemModifiedBy), value, 0);
        }

        /// <summary>
        /// Item modified when.
        /// </summary>
        [DatabaseField]
        public virtual DateTime ItemModifiedWhen
        {
            get => ValidationHelper.GetDateTime(
                GetValue(nameof(ItemModifiedWhen)),
                DateTimeHelper.ZERO_TIME);
            set => SetValue(
                nameof(ItemModifiedWhen),
                value,
                DateTimeHelper.ZERO_TIME);
        }

        /// <summary>
        /// Item order.
        /// </summary>
        [DatabaseField]
        public virtual int ItemOrder
        {
            get => ValidationHelper.GetInteger(GetValue(nameof(ItemOrder)), 0);
            set => SetValue(nameof(ItemOrder), value, 0);
        }

        /// <summary>
        /// Item GUID.
        /// </summary>
        [DatabaseField]
        public virtual Guid ItemGUID
        {
            get => ValidationHelper.GetGuid(GetValue(nameof(ItemGUID)), Guid.Empty);
            set => SetValue(nameof(ItemGUID), value);
        }

        /// <summary>
        /// Name.
        /// </summary>
        [DatabaseField]
        public virtual string Name
        {
            get => ValidationHelper.GetString(GetValue(nameof(Name)), string.Empty);
            set => SetValue(nameof(Name), value);
        }

        /// <summary>
        /// Deletes the object using the appropriate provider.
        /// </summary>
        protected override void DeleteObject()
        {
            Provider.Delete(this);
        }

        /// <summary>
        /// Saves the object and maintains audit fields.
        /// </summary>
        protected override void SetObject()
        {
            int currentUserId =
                MembershipContext.AuthenticatedUser?.UserID ?? 0;

            DateTime now = DateTime.Now;

            // CREATE: ContactTagsID is 0 before the first save because it is an IDENTITY column.
            if (ContactTagsID <= 0)
            {
                ItemCreatedBy = currentUserId;
                ItemCreatedWhen = now;

                ItemModifiedBy = 0;
                ItemModifiedWhen = DateTimeHelper.ZERO_TIME;

                if (ItemGUID == Guid.Empty)
                {
                    ItemGUID = Guid.NewGuid();
                }

                // First save: SQL Server generates ContactTagsID.
                Provider.Set(this);

                // Set ItemOrder after ContactTagsID has been generated.
                ItemOrder = ContactTagsID;

                // Second save persists ItemOrder.
                Provider.Set(this);

                return;
            }

            // UPDATE: maintain modified-by and modified-when values.
            ItemModifiedBy = currentUserId;
            ItemModifiedWhen = now;

            Provider.Set(this);
        }

        /// <summary>
        /// Creates an empty instance of ContactTagsInfo.
        /// </summary>
        public ContactTagsInfo()
            : base(TYPEINFO)
        {
        }

        /// <summary>
        /// Creates an instance of ContactTagsInfo from a DataRow.
        /// </summary>
        public ContactTagsInfo(DataRow dr)
            : base(TYPEINFO, dr)
        {
        }
    }
}
