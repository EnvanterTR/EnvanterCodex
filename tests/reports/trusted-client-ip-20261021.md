# Trusted client IP resolver — 21 October2026

Implemented opt-in resolver for inspected client/Cloudflare→Traefik→Nginx→app topology. Requires configured ingress CIDRs for both immediate peer and last forwarded proxy. Consumes final two hops; ignores left prefixes. CF-Connecting-IP accepted only when outer source is in official Cloudflare networks. IPv4-mapped IPv6 normalized; malformed/missing/duplicate values leave original peer. Global /0 trust rejected. Empty configuration leaves existing behavior unchanged.

Official ranges verified from https://www.cloudflare.com/ips-v4 and https://www.cloudflare.com/ips-v6 on21October2026. No runtime downloads.

Unit acceptance: Docker --network none using existing test image, current Program/resolver/test mounted readonly, dotnet test --no-restore --filter FullyQualifiedName~TrustedClientIpTests.17/17PASS, process50781exit0. No database or production configuration changed. Compose adds empty/false opt-in environment defaults; no automatic production enablement.

Remaining: actual HTTP pipeline/session/rate-limit tests and external forged-header acceptance; production configured ingress CIDR validation and release. This is not a claim that live IP records are fixed.
