window.fetchBuildInfo = async function () {
    try {
        const response = await fetch(`buildinfo.json?ts=${Date.now()}`);
        if (!response.ok) throw new Error('Failed to fetch buildinfo.json');
        const payload = await response.json();
        return {
            ReleaseVersion: payload.ReleaseVersion ?? 'Unknown',
            BuildDate: payload.BuildDate ?? 'Unknown',
            DotNetVersion: payload.DotNetVersion ?? 'Unknown'
        };
    } catch {
        return { ReleaseVersion: 'Unknown', BuildDate: 'Unknown', DotNetVersion: 'Unknown' };
    }
};
