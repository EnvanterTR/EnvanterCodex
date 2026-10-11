# Third-party notices

## Inter typeface

The web application and public site bundle the Inter 4.1 variable WOFF2 font,
from the official <https://github.com/rsms/inter/releases/tag/v4.1> release.
The SIL Open Font License 1.1 text is retained beside each deployed font as
`Inter-OFL.txt` under both `src/EnvanterTR.Web/wwwroot/assets/fonts/` and
`src/EnvanterTR.PublicSite/assets/fonts/`.

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

The inventory is a release-review input, not a legal conclusion. Windows CI
checks the Windows RID dependency manifest produced by the same self-contained
publish invocation; the manifest is bundled into the single-file executable
and is not emitted as a separate release file. Linux CI extracts the final
application image's dependency manifest. Both are checked against the web
application's inventory rows by `tools/Verify-PublishedDependencies.ps1`; the
check fails if a published NuGet package has no inventory record or license
reference. The current local build reports 37 Windows RID package libraries
and 12 Linux image package libraries, all mapped to recorded license metadata.

These package-manifest checks do not identify or review every license notice
embedded within a bundled binary, replace human licensing/attribution review,
or establish license compatibility. Rebuild the inventory after changing
project package references or vendored assets.
