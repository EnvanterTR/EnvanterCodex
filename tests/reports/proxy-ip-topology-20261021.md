# Production client IP topology — 21 October 2026

Observed from Docker metadata only; credentials/environment not read.
- App resource network izrckbfan6xop8b6xgigduvt: subnet10.0.2.0/24, gateway10.0.2.1.
- Web10.0.2.3, public Nginx10.0.2.4, Traefik10.0.2.5. Web/public also attach to separate internal network10.0.3.x. Neither publishes host ports.
- Traefik publishes80/443. Current Args do not configure forwardedHeaders.insecure or trustedIPs.
- Public DNS A addresses104.21.10.9 and172.67.189.218 show the request path includes Cloudflare.
- Nginx app/API routes append proxy_add_x_forwarded_for; ASP.NET processes forwarded protocol only, leaving session/rate-limit IP at Nginx10.0.2.4.

Safe next implementation must restrict forwarded IP processing to the application ingress network and authenticate the Cloudflare source before considering CF-Connecting-IP. Never use an arbitrary inbound header as the client address. Direct-origin forged CF headers and untrusted direct app peers need negative tests. No proxy trust or production config changed in this investigation.

Screenshotproxy-ip-topology-20261021.png. IP correction not yet implemented.
