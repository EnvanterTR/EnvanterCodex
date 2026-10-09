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
tooling dependencies. A resolved-version snapshot of all 40 direct and
transitive NuGet packages in the web project, including each package's license
expression and NuGet license URL, is recorded in
[`THIRD-PARTY-DEPENDENCIES.csv`](THIRD-PARTY-DEPENDENCIES.csv). The snapshot
uses package `.nuspec` metadata; it is an inventory aid, not a legal review or
a replacement for notices shipped by each dependency. Recheck it whenever
package versions change and distinguish build-only packages from files in each
platform's published output.
