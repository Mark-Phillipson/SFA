using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace SFA_PWA.Services
{
    /// <summary>
    /// Service to detect network connectivity status
    /// </summary>
    public class NetworkStatusService : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private DotNetObjectReference<NetworkStatusService>? _objRef;

        public NetworkStatusService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public event EventHandler<bool>? OnlineStatusChanged;

        public async Task<bool> IsOnlineAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>("eval", "navigator.onLine");
            }
            catch
            {
                // Assume online if we can't check
                return true;
            }
        }

        public async Task InitializeAsync()
        {
            _objRef = DotNetObjectReference.Create(this);
            await _jsRuntime.InvokeVoidAsync("eval", $@"
                window.addEventListener('online', () => {{
                    if (window.networkStatusServiceRef) {{
                        window.networkStatusServiceRef.invokeMethodAsync('NotifyOnlineStatusChanged', true);
                    }}
                }});
                window.addEventListener('offline', () => {{
                    if (window.networkStatusServiceRef) {{
                        window.networkStatusServiceRef.invokeMethodAsync('NotifyOnlineStatusChanged', false);
                    }}
                }});
                window.networkStatusServiceRef = arguments[0];
            ", _objRef);
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
                await _jsRuntime.InvokeVoidAsync("eval", "delete window.networkStatusServiceRef;");
                _objRef.Dispose();
            }
        }
    }
}
