using System;

namespace PeakboardExtensionGraph
{

    public class ContactList
    {
        public string odatacontext { get; set; }
        public Contact[] value { get; set; }
    }

    public class Contact
    {
        public string odataetag { get; set; }
        public string id { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public string changeKey { get; set; }
        public object[] categories { get; set; }
        public string parentFolderId { get; set; }
        public object birthday { get; set; }
        public string fileAs { get; set; }
        public string displayName { get; set; }
        public string givenName { get; set; }
        public string initials { get; set; }
        public string middleName { get; set; }
        public object nickName { get; set; }
        public string surname { get; set; }
        public string title { get; set; }
        public object yomiGivenName { get; set; }
        public object yomiSurname { get; set; }
        public object yomiCompanyName { get; set; }
        public string generation { get; set; }
        public string[] imAddresses { get; set; }
        public string jobTitle { get; set; }
        public string companyName { get; set; }
        public string department { get; set; }
        public object officeLocation { get; set; }
        public object profession { get; set; }
        public object businessHomePage { get; set; }
        public object assistantName { get; set; }
        public object manager { get; set; }
        public object[] homePhones { get; set; }
        public object mobilePhone { get; set; }
        public object[] businessPhones { get; set; }
        public object spouseName { get; set; }
        public string personalNotes { get; set; }
        public object[] children { get; set; }
        public Emailaddress[] emailAddresses { get; set; }
        public Homeaddress homeAddress { get; set; }
        public Businessaddress businessAddress { get; set; }
        public Otheraddress otherAddress { get; set; }
    }

    public class Homeaddress
    {
    }

    public class Businessaddress
    {
        public string countryOrRegion { get; set; }

        public override string ToString()
        {
            return countryOrRegion;
        }
    }

    public class Otheraddress
    {
    }

    public class Emailaddress
    {
        public string name { get; set; }
        public string address { get; set; }

        public override string ToString()
        {
            return address;
        }
    }

}