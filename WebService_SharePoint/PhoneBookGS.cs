using System;
using System.Xml.Serialization;
using System.Collections.Generic;
namespace New_GS
{
	[XmlRoot(ElementName="pbgroup")]
	public class Pbgroup {
		[XmlElement(ElementName="id")]
		public string Id { get; set; }
		[XmlElement(ElementName="name")]
		public string Name { get; set; }
	}

	[XmlRoot(ElementName="Phone")]
	public class Phone {
		[XmlElement(ElementName="phonenumber")]
		public string Phonenumber { get; set; }
		[XmlElement(ElementName="accountindex")]
		public string Accountindex { get; set; }
		[XmlAttribute(AttributeName="type")]
		public string Type { get; set; }
	}

	[XmlRoot(ElementName="Contact")]
	public class Contact {
		[XmlElement(ElementName="id")]
		public string Id { get; set; }
		[XmlElement(ElementName="FirstName")]
		public string FirstName { get; set; }
		[XmlElement(ElementName="LastName")]
		public string LastName { get; set; }
		[XmlElement(ElementName="Frequent")]
		public string Frequent { get; set; }
		[XmlElement(ElementName="Phone")]
		public List<Phone> Phone { get; set; }
		[XmlElement(ElementName="Group")]
		public string Group { get; set; }
		[XmlElement(ElementName="Primary")]
		public string Primary { get; set; }
		[XmlElement(ElementName="Company")]
		public string Company { get; set; }
		[XmlElement(ElementName="JobTitle")]
		public string JobTitle { get; set; }
		[XmlElement(ElementName="Department")]
		public string Department { get; set; }
		[XmlElement(ElementName="Job")]
		public string Job { get; set; }
	}

	[XmlRoot(ElementName="AddressBook")]
	public class AddressBook {
		[XmlElement(ElementName="pbgroup")]
		public List<Pbgroup> Pbgroup { get; set; }
		[XmlElement(ElementName="Contact")]
		public List<Contact> Contact { get; set; }
	}

}