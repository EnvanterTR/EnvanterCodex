# Coolify deployment

Use `deploy/coolify/compose.yaml` as the Docker Compose file in a Git-based Coolify application. Keep the base directory at `/` so the web and public-site Docker build contexts can read the repository root.

The stack starts a new PostgreSQL database, the EnvanterTR application, and the public marketing/demo site. The web application and public Nginx service share a private Compose network so `/giris` and `/api` can reach the app; PostgreSQL is on that same non-published network. It uses three explicitly named, EnvanterTR v1-specific persistent volumes for database contents, uploaded company media, and ASP.NET data-protection keys. Those volume names are independent of the Coolify project/resource slug, so a similarly named legacy service cannot silently supply its database or files. Do not rename them to legacy volume names or point this stack at another application's database.

Coolify generates `SERVICE_PASSWORD_64_POSTGRES` and `SERVICE_PASSWORD_64_SETUP` for the database password and one-time initial-owner setup token. Keep both values in Coolify's environment-variable store. The bootstrap token is required to create the first owner and should be removed from the application environment after setup. SMTP variables are optional and can be added later for email delivery.

In Coolify, add a domain to the `public-site` service on port 80 after the stack is deployed and verified. The public site proxies `/giris`, setup pages, and `/api` to the private `web` service. The PostgreSQL service has no public port.
