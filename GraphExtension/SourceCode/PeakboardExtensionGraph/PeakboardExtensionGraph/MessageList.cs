using System;

namespace PeakboardExtensionGraph
{

    public class MessageList
    {
        public string odatacontext { get; set; }
        public string odatanextLink { get; set; }
        public EmailMessage[] value { get; set; }
    }

    public class EmailMessage
    {
        public string odataetag { get; set; }
        public string id { get; set; }
        public DateTime createdDateTime { get; set; }
        public DateTime lastModifiedDateTime { get; set; }
        public string changeKey { get; set; }
        public object[] categories { get; set; }
        public DateTime receivedDateTime { get; set; }
        public DateTime sentDateTime { get; set; }
        public bool hasAttachments { get; set; }
        public string internetMessageId { get; set; }
        public string subject { get; set; }
        public string bodyPreview { get; set; }
        public string importance { get; set; }
        public string parentFolderId { get; set; }
        public string conversationId { get; set; }
        public string conversationIndex { get; set; }
        public bool? isDeliveryReceiptRequested { get; set; }
        public bool isReadReceiptRequested { get; set; }
        public bool isRead { get; set; }
        public bool isDraft { get; set; }
        public string webLink { get; set; }
        public string inferenceClassification { get; set; }
        public Body body { get; set; }
        public MailAddressContext sender { get; set; }
        public MailAddressContext from { get; set; }
        public MailAddressContext[] toRecipients { get; set; }
        public object[] ccRecipients { get; set; }
        public object[] bccRecipients { get; set; }
        public MailAddressContext[] replyTo { get; set; }
        public Flag flag { get; set; }
        public string odatatype { get; set; }
        public string meetingMessageType { get; set; }
        public string type { get; set; }
        public bool isOutOfDate { get; set; }
        public bool isAllDay { get; set; }
        public bool isDelegated { get; set; }
        public string responseType { get; set; }
        public object recurrence { get; set; }
        public Startdatetime startDateTime { get; set; }
        public Enddatetime endDateTime { get; set; }
        public Location location { get; set; }
        public bool responseRequested { get; set; }
        public object allowNewTimeProposals { get; set; }
        public string meetingRequestType { get; set; }
        public object previousLocation { get; set; }
        public object previousStartDateTime { get; set; }
        public object previousEndDateTime { get; set; }
    }

    public class Body
    {
        public string contentType { get; set; }
        public string content { get; set; }
    }

    public class MailAddressContext
    {
        public Emailaddress emailAddress { get; set; }
        
        public override string ToString()
        {
            return emailAddress.address;
        }
    }

    public class Flag
    {
        public string flagStatus { get; set; }
    }

    public class Startdatetime
    {
        public DateTime dateTime { get; set; }
        public string timeZone { get; set; }

        public override string ToString()
        {
            return $"{dateTime.ToString()} {timeZone}";
        }
    }

    public class Enddatetime
    {
        public DateTime dateTime { get; set; }
        public string timeZone { get; set; }
    }

    public class Location
    {
        public string displayName { get; set; }
        public string locationType { get; set; }
        public string uniqueIdType { get; set; }
    }


    //-------------- Post Request Message --------------
    public class Rootobject
    {
        public SendMessage message { get; set; }
    }

    public class SendMessage
    {
        public string subject { get; set; }
        public Body body { get; set; }
        public Torecipient[] toRecipients { get; set; }
    }

    public class Torecipient
    {
        public Emailaddress emailAddress { get; set; }
    }


}