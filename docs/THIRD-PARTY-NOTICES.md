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

## Resolved NuGet dependencies

`Microsoft.EntityFrameworkCore.Design` is used by the Entity Framework CLI for
development and migration generation. It is marked `PrivateAssets=all` and is
excluded from Release deployment output together with its MSBuild and Roslyn
tooling dependencies. The generated snapshot in
[`THIRD-PARTY-DEPENDENCIES.csv`](THIRD-PARTY-DEPENDENCIES.csv) records every
resolved package separately for the web application, security acceptance
project, and backup command-line project. Each NuGet record includes whether it
is a direct or transitive project dependency, its resolved version, and license
expression, URL, or package license-file reference read from its `.nuspec`.
The generator runs in Windows CI and the job fails if the committed inventory
is stale. The same file also lists the vendored QR/barcode libraries and the
license notices retained beside them.

This generated inventory is a release-review input, not a legal conclusion. It
does not determine whether every resolved package is present in each platform's
published output, replace package-specific notices, or establish license
compatibility. Those checks still require review of the actual Windows and
Linux publish artifacts and a human licensing/attribution review. Rebuild the
inventory after changing project package references or vendored assets.
