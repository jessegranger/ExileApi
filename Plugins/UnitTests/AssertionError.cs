using System;
using System.Runtime.Serialization;

namespace UnitTests
{
    [Serializable]
    internal class AssertionError : Exception
    {
        public AssertionError()
        {
        }

        public AssertionError(string message) : base(message)
        {
        }

        public AssertionError(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected AssertionError(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}