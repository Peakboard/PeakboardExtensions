using System;
using System.IO.BACnet;
using BacNetExtension.CustomLists.Exceptions;

namespace BacNetExtension.CustomLists.Helpers
{
    public static class BacNetValidationHelper
    {
        public static void ValidateAddress(BacnetAddress address)
        {
            if (address == null)
                throw new BacNetValidationException("Address cannot be null");
        }

        public static void ValidateObjectId(BacnetObjectId objectId)
        {
            if (objectId == null)
                throw new BacNetValidationException("Object ID cannot be null");
        }

        public static void ValidatePropertyId(BacnetPropertyIds propertyId)
        {
            if (!Enum.IsDefined(typeof(BacnetPropertyIds), propertyId))
                throw new BacNetValidationException($"Invalid property ID: {propertyId}");
        }

        public static void ValidateInstanceNumber(string instance)
        {
            if (string.IsNullOrWhiteSpace(instance))
                throw new BacNetValidationException("Instance number cannot be empty");

            if (!uint.TryParse(instance, out _))
                throw new BacNetValidationException($"Invalid instance number format: {instance}");
        }

        public static void ValidatePort(int port)
        {
            if (port <= 0 || port > 65535)
                throw new BacNetValidationException($"Invalid port number: {port}");
        }
    }
} 