using System;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Tests.TestHelpers
{
    [ExcludeFromCodeCoverage]
    public class TestException : Exception
    {
        public TestException()
        {
            
        }

        public TestException(string message) : base(message)
        {
            
        }
    }
}
