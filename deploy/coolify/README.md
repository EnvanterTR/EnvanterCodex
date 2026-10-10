# Coolify deployment

Use `deploy/coolify/compose.yaml` as the Docker Compose file in a Git-based Coolify application. Keep the base directory at `/` so the web and public-site Docker build contexts can read the repository root.

The stack starts a new PostgreSQL database, the EnvanterTR application, and the public marketing/demo site. It uses independent persistent volumes for database contents, uploaded company media, and ASP.NET data-protection keys. Do not point this stack at the database belonging to another application.

Coolify generates `SERVICE_PASSWORD_64_POSTGRES` and `SERVICE_PASSWORD_64_SETUP` for the database password and one-time initial-owner setup token. Keep both values in Coolify's environment-variable store. The bootstrap token is required to create the first owner and should be removed from the application environment after setup. SMTP variables are optional and can be added later for email delivery.

In Coolify, add a domain to the `public-site` service on port 80 after the stack is deployed and verified. The public site proxies `/giris`, setup pages, and `/api` to the private `web` service. The PostgreSQL service has no public port.
