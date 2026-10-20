# Local camera decoder

- Package: `@zxing/browser` 0.2.1, official npm registry archive `https://registry.npmjs.org/@zxing/browser/-/browser-0.2.1.tgz`.
- Archive integrity checked before extracting: `sha512-92pVfVDUXbc15xu9vIEmhNyqIEASMEkDF92CwT6T+7sg2EHXJRktH9CQKoQSXe51UtbNsj+Fm09H/JBWHMs/Sw==`.
- Vendored file: `package/umd/zxing-browser.min.js`, renamed `zxing-browser-0.2.1.min.js`.
- Browser layer license: `ZXING-BROWSER-LICENSE` (MIT).
- Bundled ZXing decoding library license: `ZXING-LIBRARY-LICENSE` (Apache 2.0), from the official `zxing-js/library` repository.
- Upstream: https://github.com/zxing-js/browser and https://github.com/zxing-js/library.

Loaded from the application origin only when native BarcodeDetector cannot initialize. Camera frames stay in the browser; the decoder does not use a remote image service.
