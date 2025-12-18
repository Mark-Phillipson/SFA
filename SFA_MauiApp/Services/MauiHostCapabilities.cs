using SFA_PWA.Services;

namespace SFA_MauiApp.Services;

public sealed class MauiHostCapabilities : ISfaHostCapabilities
{
	public bool SupportsPwaInstall => false;
	public bool SupportsBrowserNetworkEvents => false;
}
