using System;

namespace BacNetExtension.CustomLists.Exceptions
{
    public class BacNetConnectionException : Exception
    {
        public BacNetConnectionException(string message) : base(message) { }
        public BacNetConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class BacNetPropertyException : Exception
    {
        public BacNetPropertyException(string message) : base(message) { }
        public BacNetPropertyException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class BacNetValidationException : Exception
    {
        public BacNetValidationException(string message) : base(message) { }
        public BacNetValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
} 