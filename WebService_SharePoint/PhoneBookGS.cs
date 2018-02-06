using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebService_SharePoint
{

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class AddressBook
    {

        private AddressBookPbgroup pbgroupField;

        private AddressBookContact[] contactField;

        /// <remarks/>
        public AddressBookPbgroup pbgroup
        {
            get
            {
                return this.pbgroupField;
            }
            set
            {
                this.pbgroupField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Contact")]
        public AddressBookContact[] Contact
        {
            get
            {
                return this.contactField;
            }
            set
            {
                this.contactField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AddressBookPbgroup
    {

        private byte idField;

        private string nameField;

        private object ringtonesField;

        /// <remarks/>
        public byte id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        public object ringtones
        {
            get
            {
                return this.ringtonesField;
            }
            set
            {
                this.ringtonesField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AddressBookContact
    {

        private string lastNameField;

        private object firstNameField;

        private bool isPrimaryField;

        private byte primaryField;

        private byte frequentField;

        private object photoUrlField;

        private AddressBookContactPhone phoneField;

        /// <remarks/>
        public string LastName
        {
            get
            {
                return this.lastNameField;
            }
            set
            {
                this.lastNameField = value;
            }
        }

        /// <remarks/>
        public object FirstName
        {
            get
            {
                return this.firstNameField;
            }
            set
            {
                this.firstNameField = value;
            }
        }

        /// <remarks/>
        public bool IsPrimary
        {
            get
            {
                return this.isPrimaryField;
            }
            set
            {
                this.isPrimaryField = value;
            }
        }

        /// <remarks/>
        public byte Primary
        {
            get
            {
                return this.primaryField;
            }
            set
            {
                this.primaryField = value;
            }
        }

        /// <remarks/>
        public byte Frequent
        {
            get
            {
                return this.frequentField;
            }
            set
            {
                this.frequentField = value;
            }
        }

        /// <remarks/>
        public object PhotoUrl
        {
            get
            {
                return this.photoUrlField;
            }
            set
            {
                this.photoUrlField = value;
            }
        }

        /// <remarks/>
        public AddressBookContactPhone Phone
        {
            get
            {
                return this.phoneField;
            }
            set
            {
                this.phoneField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class AddressBookContactPhone
    {

        private string phonenumberField;

        private sbyte accountindexField;

        private string typeField;

        /// <remarks/>
        public string phonenumber
        {
            get
            {
                return this.phonenumberField;
            }
            set
            {
                this.phonenumberField = value;
            }
        }

        /// <remarks/>
        public sbyte accountindex
        {
            get
            {
                return this.accountindexField;
            }
            set
            {
                this.accountindexField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }


}