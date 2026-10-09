# Third-party notices

## QR Code generator library

The files `src/EnvanterTR.Web/wwwroot/vendor/qrcodegen.ts` and
`src/EnvanterTR.Web/wwwroot/vendor/qrcodegen.js` contain the TypeScript QR Code
generator by Project Nayuki, sourced from the project's official repository:
<https://github.com/nayuki/QR-Code-generator>.

Copyright (c) Project Nayuki. Distributed under the MIT License. The complete
license notice is retained in both vendored files.

The file `src/EnvanterTR.Web/wwwroot/vendor/bwip-js-min.js` contains bwip-js
4.11.4, sourced from <https://github.com/metafloor/bwip-js>. It is distributed
under the MIT License. The package's license notices for bwip-js and its bundled
components are retained beside the file as `BWIP-JS-LICENSE*`.

## Build-time dependencies

`Microsoft.EntityFrameworkCore.Design` is used by the Entity Framework CLI for
development and migration generation. It is marked `PrivateAssets=all` and is
excluded from Release deployment output together with its MSBuild and Roslyn
tooling dependencies. The complete runtime and development dependency license
inventory still requires release review.
