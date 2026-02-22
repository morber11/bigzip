using BigZipUI.Services;
using System;

namespace BigZipUI.Tests.Helpers
{
    // wrapper for testing
    public class SynchronousDispatcher : IDispatcher
    {
        public void Post(Action action) => action();
    }
}
