const URLS = {
  BFF_URL: 'http://localhost:5000',
  ZITADEL_URL: 'http://localhost:9080'
};

// For Node (proxy.conf.js)
if (typeof module !== 'undefined' && module.exports) {
  module.exports = URLS;
}

// For browser (TypeScript can import)
if (typeof window !== 'undefined') {
  window.APP_URLS = URLS;
}
