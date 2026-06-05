using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace SFA_RazorClassLibrary.Services
{
    /// <summary>
    /// Service to detect network connectivity status
    /// </summary>
    public class NetworkStatusService : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ISfaHostCapabilities _hostCapabilities;
        private DotNetObjectReference<NetworkStatusService>? _objRef;

        public NetworkStatusService(IJSRuntime jsRuntime, ISfaHostCapabilities hostCapabilities)
        {
            _jsRuntime = jsRuntime;
            _hostCapabilities = hostCapabilities;
        }

        public event EventHandler<bool>? OnlineStatusChanged;

        public async Task<bool> IsOnlineAsync()
        {
            if (!_hostCapabilities.SupportsBrowserNetworkEvents)
            {
                // Treat unknown as online to avoid showing a false offline banner.
                return true;
            }

            try
            {
                return await _jsRuntime.InvokeAsync<bool>("networkStatusHelper.isOnline");
            }
            catch
            {
                // Assume online if we can't check
                return true;
            }
        }

        public async Task InitializeAsync()
        {
            if (!_hostCapabilities.SupportsBrowserNetworkEvents)
            {
                return;
            }

            if (_objRef != null)
            {
                return;
            }

            _objRef = DotNetObjectReference.Create(this);

            try
            {
                await _jsRuntime.InvokeVoidAsync("networkStatusHelper.initialize", _objRef);
            }
            catch (JSException)
            {
                // No-op (e.g., host didn't include the JS helper).
            }
            catch (InvalidOperationException)
            {
                // No-op (e.g., JS runtime not available yet).
            }
        }

        [JSInvokable]
        public Task NotifyOnlineStatusChanged(bool isOnline)
        {
            OnlineStatusChanged?.Invoke(this, isOnline);
            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_objRef != null)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("networkStatusHelper.dispose");
                }
                catch (JSException)
                {
                    // Ignore.
                }
                catch (InvalidOperationException)
                {
                    // Ignore.
                }
                _objRef.Dispose();
                _objRef = null;
            }
        }
    }
}
