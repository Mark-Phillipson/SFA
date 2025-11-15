// In development, we don't need a no-op fetch handler. Removing this prevents a
// browser warning about no-op fetch handlers adding overhead during navigation.
// Note: if you intentionally need to intercept fetch for caching, add a real
// handler here that implements caching behavior.
