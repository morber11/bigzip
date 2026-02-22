using Avalonia.Threading;
using System;

namespace BigZipUI.Services
{
    // purely a wrapper around the main Avalonia dispatcher for testing
    public class AvaloniaDispatcher : IDispatcher
    {
        public void Post(Action action) => Dispatcher.UIThread.Post(action);
    }
}
