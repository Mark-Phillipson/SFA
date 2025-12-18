namespace SFA_PWA.Services;

public interface ISfaHostCapabilities
{
	bool SupportsPwaInstall { get; }
	bool SupportsBrowserNetworkEvents { get; }
}

public sealed class BrowserHostCapabilities : ISfaHostCapabilities
{
	public bool SupportsPwaInstall => true;
	public bool SupportsBrowserNetworkEvents => true;
}
