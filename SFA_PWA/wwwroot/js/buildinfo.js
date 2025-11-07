window.fetchBuildInfo = async function () {
    try {
        const response = await fetch('buildinfo.json');
        if (!response.ok) throw new Error('Failed to fetch buildinfo.json');
        return await response.json();
    } catch {
        return { DotNetVersion: 'Unknown', BuildDate: 'Unknown' };
    }
};
