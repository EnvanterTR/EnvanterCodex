# Coolify deployment

Use `deploy/coolify/compose.yaml` as the Docker Compose file in a Git-based Coolify application. Keep the base directory at `/` so the web and public-site Docker build contexts can read the repository root.

The stack starts a new PostgreSQL database, the EnvanterTR application, and the public marketing/demo site. The web application and public Nginx service share a private Compose network so `/giris` and `/api` can reach the app; PostgreSQL is on that same non-published network. It uses three explicitly named, EnvanterTR v1-specific persistent volumes for database contents, uploaded company media, and ASP.NET data-protection keys. Those volume names are independent of the Coolify project/resource slug, so a similarly named legacy service cannot silently supply its database or files. Do not rename them to legacy volume names or point this stack at another application's database.

## Request traffic and GitHub are separate

Customer browsers connect to the configured `envantertr.com` domain, which Coolify routes to `public-site`; Nginx sends application routes to `web`, and `web` talks to PostgreSQL on the private Compose network. Normal page views, sign-ins, inventory operations, and customer uploads are served by this server stack. They do not start GitHub Actions jobs or consume GitHub Actions minutes. GitHub stores the source and sends a deployment change signal; Coolify fetches/builds a new revision when a deployment is triggered. If no deployment runs, the currently deployed revision continues serving traffic.

The repository's `EnvanterTR CI` workflow is separate from Coolify deployment. It runs on pushes to `main` and pull requests targeting `main`, subject to GitHub account/plan limits. A failed or quota-blocked CI run means that CI check did not complete; it does not by itself stop the already-running Coolify stack. Do not add a payment method just to keep the existing website serving requests. If included Actions capacity is exhausted, wait for its reset or use a separately approved runner/deployment process; paid usage is an account billing choice, not a prerequisite for ordinary customer traffic.

Database schema changes are a separate operational step. The application does not automatically apply pending migrations during normal startup; take and verify a backup, then explicitly run the approved migration procedure before expecting an older production database to match a newer application schema.

Coolify generates `SERVICE_PASSWORD_64_POSTGRES` and `SERVICE_PASSWORD_64_SETUP` for the database password and one-time initial-owner setup token. Keep both values in Coolify's environment-variable store. The bootstrap token is required to create the first owner and should be removed from the application environment after setup. SMTP variables are optional and can be added later for email delivery.

In Coolify, add a domain to the `public-site` service on port 80 after the stack is deployed and verified. The public site proxies `/giris`, setup pages, and `/api` to the private `web` service. The PostgreSQL service has no public port.
