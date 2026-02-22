using System;

namespace BigZipUI.Services
{
    // exclusively as a wrapper for easier testing - we can intercept calls to the avalonia dispatcher
    public interface IDispatcher
    {
        void Post(Action action);
    }
}
