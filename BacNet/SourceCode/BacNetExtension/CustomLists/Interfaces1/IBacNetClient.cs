using System.IO.BACnet;

namespace BacNetExtension.CustomLists.Interfaces
{
    public interface IBacNetClient
    {
        void Start();
        void Dispose();
        bool ReadPropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, out IList<BacnetValue> values);
        bool ReadPropertyMultipleRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyReference[] properties, out IList<BacnetPropertyValue> values);
        bool WritePropertyRequest(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, BacnetValue[] values);
        bool SubscribeCOVRequest(BacnetAddress address, BacnetObjectId objectId, uint subscriberProcessIdentifier, bool issueConfirmedNotifications, bool lifetime, uint lifetimeValue);
        void SimpleAckResponse(BacnetAddress address, BacnetConfirmedServices service, byte invokeId);
        event BacnetClient.COVNotificationHandler OnCOVNotification;
    }
} 