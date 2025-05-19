using System;

namespace BacNetExtension.CustomLists.Helpers;

public class BacNetLoggingHelper
{
    private readonly Action<string,string> _log;
    public BacNetLoggingHelper(Action<string,string> log)
    {
        _log = log;
    }

    public void LogWarning(string message)
    {
        _log.Invoke("warning",$"Warning: {message}");
    }

    public void LogError(string message, Exception exception)
    {
        _log.Invoke("error", $"Error: {message}, Exception: {exception}");
    }

    public void LogInfo(string message)
    {
        _log.Invoke("info", $"Info: {message}");
    }
}