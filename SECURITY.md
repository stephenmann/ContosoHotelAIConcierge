# Contoso Hotels Security Guidelines

## Handling Sensitive Information

### Environment Variables
The Contoso Hotels application uses environment variables to store sensitive information such as database credentials. This approach ensures that:

1. Sensitive data is not committed to source control
2. Different environments can use different credentials
3. Credentials can be managed centrally in deployment environments

### Local Development Setup

For local development:

1. Copy `.env.example` to `.env` in the root directory
2. Set a secure password in the `.env` file
3. Use the `load-env.ps1` script to load these variables in PowerShell
4. Never commit the `.env` file to Git

### Docker Development

When using Docker:

1. Copy `.devcontainer/.env.example` to `.devcontainer/.env`
2. Set a secure password in the file
3. The Docker Compose setup will automatically use these credentials

### Production Deployment

For production environments:

1. Use a secure secret management service appropriate for your hosting platform
2. Set environment variables through your deployment pipeline
3. Rotate credentials regularly
4. Consider using managed identity when deploying to cloud platforms

## Database Connection Strings

Database connection strings are managed as follows:

1. Template connection strings with placeholders are stored in `appsettings.json` and `appsettings.Development.json.example`
2. Actual connection strings with credentials are stored in `appsettings.Development.json` (not committed to Git)
3. At runtime, placeholders like `${SQL_PASSWORD}` are replaced with actual values from environment variables

## General Security Best Practices

1. Use parameterized queries to prevent SQL injection
2. Validate all user inputs
3. Implement HTTPS for all communications
4. Follow the principle of least privilege for database access
5. Keep dependencies updated to patch security vulnerabilities
