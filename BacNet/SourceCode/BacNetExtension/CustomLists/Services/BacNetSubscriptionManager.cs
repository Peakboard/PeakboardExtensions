using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Timers;
using BacNetExtension.CustomLists.Сonstants;
using BacNetExtension.CustomLists.Helpers;

namespace BacNetExtension.CustomLists.Services
{
    public class BacNetSubscriptionManager
    {
        private readonly BacnetClient _client;
        private readonly Dictionary<string, BacnetObjectTypes> _objectMap;
        private readonly Action<string, string> _logCallback;
        private readonly Dictionary<uint, Timer> _covTimers;
        private uint _subscriptionIdCounter;
        private readonly BacNetLoggingHelper _bacNetLoggingHelper;
        
        public BacNetSubscriptionManager(BacnetClient client, Dictionary<string, BacnetObjectTypes> objectMap,Action<string,string> logCallback)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _objectMap = objectMap ?? throw new ArgumentNullException(nameof(objectMap));
            _logCallback = logCallback;
            _covTimers = new Dictionary<uint, Timer>();
            _subscriptionIdCounter = 0;
            _bacNetLoggingHelper = new BacNetLoggingHelper(logCallback);
        }

        public void SubscribeToObjects(BacnetAddress address, string objectName, string[] instances)
        {
            try
            {
                BacNetHelper.ValidateAddress(address);

                if (!_objectMap.TryGetValue(objectName, out var type))
                {
                    throw new Exception($"Invalid object name: {objectName}");
                }

                foreach (var instance in instances)
                {
                    if (!uint.TryParse(instance, out uint instanceNumber))
                    {
                        throw new Exception($"Invalid instance number: {instance}");
                    }

                    SubscribeToObject(address, type, instanceNumber);
                }
            }
            catch (Exception ex)
            {
                _bacNetLoggingHelper.LogError($"Error subscribing to objects: {ex.Message}", ex);
                throw new Exception("Failed to subscribe to objects", ex);
            }
        }

        private void SubscribeToObject(BacnetAddress address, BacnetObjectTypes type, uint instance)
        {
            var objectId = new BacnetObjectId(type, instance);
            _subscriptionIdCounter++;

            if (_client.SubscribeCOVRequest(address, objectId, _subscriptionIdCounter, false, true, ExtensionConstans.SubsriptionDuraion))
            {
                _bacNetLoggingHelper.LogInfo($"Subscription to object {objectId} established");
                SetupRenewalTimer(address, type, instance);
            }
            else
            {
                _bacNetLoggingHelper.LogWarning($"Failed to subscribe to object {objectId}");
            }
        }

        private void SetupRenewalTimer(BacnetAddress address, BacnetObjectTypes type, uint instance)
        {
            var timer = new Timer((ExtensionConstans.SubsriptionDuraion - 20) * 1000);
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

                if (_client.SubscribeCOVRequest(address, objectId, _subscriptionIdCounter, false, true, ExtensionConstans.SubsriptionDuraion))
                {
                    _bacNetLoggingHelper.LogInfo($"Renewed subscription to object {objectId}");
                }
                else
                {
                    _bacNetLoggingHelper.LogWarning($"Failed to renew subscription to object {objectId}");
                }
            }
            catch (Exception ex)
            {
                _bacNetLoggingHelper.LogError($"Error renewing subscription: {ex.Message}", ex);
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