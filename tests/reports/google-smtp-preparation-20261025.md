# Google SMTP preparation — 25 October 2026

User-selected sender: `admin@envantertr.com`. Public MX previously resolves to Google; actual account authentication/delivery has not been established.

Google's primary setup documentation supports `smtp.gmail.com`, TLS port 587 and the complete Workspace account address with an application password: https://support.google.com/a/answer/176600?hl=en (Option 2). A platform login password is not assumed to be a Google or SMTP password.

## Saved through the existing Coolify production UI

- `EMAIL_SMTP_HOST=smtp.gmail.com`
- `EMAIL_SMTP_USERNAME=admin@envantertr.com`
- `EMAIL_FROM_ADDRESS=admin@envantertr.com`
- `EMAIL_PUBLIC_BASE_URL=https://envantertr.com`

The existing Compose defaults preserve port 587, TLS enabled and sender name EnvanterTR. Preview deployment variables were not changed. The host was reopened after a full reload and its non-secret value was visibly confirmed; screenshot `coolify-google-smtp-host-saved-20261025.png`.

## Pending private credential and activation

The production `EMAIL_SMTP_PASSWORD` edit dialog was opened and its value was empty; the user was asked once to enter the Google application password directly into that protected field and save it. No secret was requested in chat, read from the UI, generated, or logged. The agent did not create a new Google credential or weaken Google authentication.

No deployment was started for this preparation. The current running 124b2d1 application still has its prior runtime environment. Saved Coolify values do not prove runtime SMTP configuration or actual delivery. Cloud trial/checkout gates were not enabled, and iyzico settings were not changed.

After the user saves the private credential: verify saved presence without reading the value, take a fresh recovery backup including current Santa/Demo3 state, deploy the same proven application SHA with the new environment, verify readiness, send an authorized real account email and obtain actual recipient-delivery evidence. Santa invitation and Demo3 restore remain open; the latter still needs fresh critical-operation proof. No application secret or server environment contents belong in public evidence.
