using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Timers;
using BacNetExtension.CustomLists.Exceptions;
using BacNetExtension.CustomLists.Helpers;
using BacNetExtension.CustomLists.Interfaces;

namespace BacNetExtension.CustomLists.Services
{
    public class BacNetSubscriptionManager
    {
        private readonly IBacNetClient _client;
        private readonly Dictionary<string, BacnetObjectTypes> _objectMap;
        private readonly Dictionary<uint, Timer> _covTimers;
        private uint _subscriptionIdCounter;
        private const uint SubscriptionDuration = 120;

        public BacNetSubscriptionManager(IBacNetClient client, Dictionary<string, BacnetObjectTypes> objectMap)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _objectMap = objectMap ?? throw new ArgumentNullException(nameof(objectMap));
            _covTimers = new Dictionary<uint, Timer>();
            _subscriptionIdCounter = 0;
        }

        public void SubscribeToObjects(BacnetAddress address, string objectName, string[] instances)
        {
            try
            {
                BacNetValidationHelper.ValidateAddress(address);

                if (!_objectMap.TryGetValue(objectName, out var type))
                {
                    throw new BacNetValidationException($"Invalid object name: {objectName}");
                }

                foreach (var instance in instances)
                {
                    if (!uint.TryParse(instance, out uint instanceNumber))
                    {
                        throw new BacNetValidationException($"Invalid instance number: {instance}");
                    }

                    SubscribeToObject(address, type, instanceNumber);
                }
            }
            catch (Exception ex)
            {
                BacNetLoggingHelper.LogError($"Error subscribing to objects: {ex.Message}", ex);
                throw new BacNetConnectionException("Failed to subscribe to objects", ex);
            }
        }

        private void SubscribeToObject(BacnetAddress address, BacnetObjectTypes type, uint instance)
        {
            var objectId = new BacnetObjectId(type, instance);
            _subscriptionIdCounter++;

            if (_client.SubscribeCOVRequest(address, objectId, _subscriptionIdCounter, false, true, SubscriptionDuration))
            {
                BacNetLoggingHelper.LogInfo($"Subscription to object {objectId} established");
                SetupRenewalTimer(address, type, instance);
            }
            else
            {
                BacNetLoggingHelper.LogWarning($"Failed to subscribe to object {objectId}");
            }
        }

        private void SetupRenewalTimer(BacnetAddress address, BacnetObjectTypes type, uint instance)
        {
            var timer = new Timer((SubscriptionDuration / 2) * 1000);
            timer.Elapsed += (sender, e) => RenewSubscription(address, type, instance);
            timer.AutoReset = true;
            timer.Start();

            _covTimers[_subscriptionIdCounter] = timer;
        }

        private void RenewSubscription(BacnetAddress address, BacnetObjectTypes type, uint instance)
        {
            try
            {
                var objectId = new BacnetObjectId(type, instance);
                _subscriptionIdCounter++;

                if (_client.SubscribeCOVRequest(address, objectId, _subscriptionIdCounter, false, true, SubscriptionDuration))
                {
                    BacNetLoggingHelper.LogInfo($"Renewed subscription to object {objectId}");
                }
                else
                {
                    BacNetLoggingHelper.LogWarning($"Failed to renew subscription to object {objectId}");
                }
            }
            catch (Exception ex)
            {
                BacNetLoggingHelper.LogError($"Error renewing subscription: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            foreach (var timer in _covTimers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            _covTimers.Clear();
        }
    }
} 