# Client IP behind the Cloud proxy chain

The verified topology is client → Cloudflare (when enabled) → Traefik → public-site Nginx → application. Nginx appends Traefik's address to X-Forwarded-For. The application must not use an arbitrary leftmost forwarded value.

Client IP forwarding stays disabled unless ForwardedHeaders:Enabled is true and ClientIp:TrustedProxyNetworks contains an explicit CIDR list. Global /0 trust is rejected. The immediate Nginx peer and final forwarded Traefik peer must both belong to that list. Other private networks are not automatically trusted.

For this inspected Coolify resource, the ingress network was 10.0.2.0/24 on 21 October2026. Reinspect Docker network metadata before configuring another deployment. Do not copy this address to unrelated servers. Web/public-site must retain no published host ports.

Set CLIENT_IP_TRUSTED_PROXY_NETWORKS to the verified application ingress CIDR and CLIENT_IP_USE_CLOUDFLARE_CONNECTING_IP=true only for the described Cloudflare path. Leave both defaults unchanged for local/direct installations. The resolver consumes the final two forwarded hops; earlier user-controlled prefixes are ignored. Traefik must retain its normal rejection of forwarded headers from untrusted external peers; never enable forwardedHeaders.insecure.

CF-Connecting-IP is considered only when the resolved outer peer belongs to Cloudflare's official ranges. Missing, duplicated or malformed connecting addresses leave the original peer unchanged. On direct-origin requests the CF header is ignored and the observed external peer is used. IPv4-mapped IPv6 peers are normalized.

Range sources verified 21 October2026: https://www.cloudflare.com/ips-v4 and https://www.cloudflare.com/ips-v6. Maintain the embedded range list when those authoritative lists change. No runtime network fetch occurs.

Acceptance still required before production enablement: resolver negative tests, actual HTTP middleware/session/rate-limit behavior, and external direct-origin spoof rejection. Source configuration alone is not proof that historical session IP records have changed; old records are preserved.
