// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => event.respondWith(onFetch(event)));

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const apiCacheName = 'api-cache-v1';
const offlineAssetsInclude = [ /\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/, /\.webmanifest$/ ];
const offlineAssetsExclude = [ /^service-worker\.js$/ ];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => offlineAssetsInclude.some(pattern => pattern.test(asset.url)))
        .filter(asset => !offlineAssetsExclude.some(pattern => pattern.test(asset.url)))
        .map(asset => new Request(asset.url, { integrity: asset.hash, cache: 'no-cache' }));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
    
    // Skip waiting to activate immediately
    self.skipWaiting();
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
    
    // Claim clients immediately
    await self.clients.claim();
}

async function onFetch(event) {
    const { request } = event;
    const url = new URL(request.url);
    
    // Only handle GET requests
    if (request.method !== 'GET') {
        return fetch(request);
    }
    
    // For navigation requests, try to serve index.html from cache
    const shouldServeIndexHtml = request.mode === 'navigate'
        && !manifestUrlList.some(manifestUrl => manifestUrl === request.url);
    
    if (shouldServeIndexHtml) {
        const cache = await caches.open(cacheName);
        const cachedResponse = await cache.match('index.html');
        if (cachedResponse) {
            return cachedResponse;
        }
    }
    
    // For API calls to external services (Google Sheets, etc.) or internal /api/ endpoints,
    // use network-first strategy with cache fallback
    if (url.hostname !== self.location.hostname || (url.hostname === self.location.hostname && url.pathname.startsWith('/api/'))) {
        try {
            const response = await fetch(request);
            // Cache successful API responses
            if (response.ok) {
                const apiCache = await caches.open(apiCacheName);
                apiCache.put(request, response.clone());
            }
            return response;
        } catch (error) {
            // If network fails, try to return cached API response
            const apiCache = await caches.open(apiCacheName);
            const cachedResponse = await apiCache.match(request);
            if (cachedResponse) {
                console.log('Serving cached API response for:', request.url);
                return cachedResponse;
            }
            throw error;
        }
    }
    
    // For static assets, use cache-first strategy
    const cache = await caches.open(cacheName);
    const cachedResponse = await cache.match(request);
    if (cachedResponse) {
        return cachedResponse;
    }
    
    // If not in cache, fetch from network and cache it
    try {
        const response = await fetch(request);
        if (response.ok) {
            cache.put(request, response.clone());
        }
        return response;
    } catch (error) {
        console.error('Fetch failed for:', request.url, error);
        throw error;
    }
}
