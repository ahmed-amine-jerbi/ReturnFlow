// Minimal service worker: it makes the app installable. It deliberately caches nothing,
// so claims and KPIs are always fresh and no customer data is stored by the worker.
self.addEventListener('install', () => self.skipWaiting())
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()))
self.addEventListener('fetch', () => {
  // Let the browser handle every request normally.
})
