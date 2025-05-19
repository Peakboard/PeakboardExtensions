using System;
using Peakboard.ExtensionKit;

namespace BacNetExtension.CustomLists.Helpers
{
    public static class BacNetLoggingHelper
    {
        public static void LogError(string message, Exception ex = null)
        {
            var logMessage = ex != null 
                ? $"{message}. Exception: {ex.Message}" 
                : message;
            Log.Error(logMessage);
        }

        public static void LogInfo(string message)
        {
            Log.Info(message);
        }

        public static void LogWarning(string message)
        {
            Log.Warning(message);
        }

        public static void LogDebug(string message)
        {
            Log.Debug(message);
        }
    }
} 