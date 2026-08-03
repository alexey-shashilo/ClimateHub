# Security Policy

## Supported Versions

Currently no stable releases. Security fixes are applied to the main branch.

## Reporting a Vulnerability

Please report security issues by email to the project maintainer rather than creating a public GitHub issue with sensitive details.

If you discover hardcoded credentials, API tokens, or private keys in the repository, **do not** commit them or post them publicly.

## Security Practices

- No secrets should be stored in tracked configuration files
- Environment variables and secret managers must be used for production credentials
- All API endpoints beyond health checks require authentication in production
- JWT signing keys must be set via environment, never in code
- MQTT anonymous access is disabled in production configuration
- Database passwords must use strong, rotated values