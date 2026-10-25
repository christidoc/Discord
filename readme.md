## Deploying to Azure

See [docs/azure-deployment.md](docs/azure-deployment.md) for the GitHub Actions
and Bicep setup for Azure Container Apps. The workflow deploys only from
`main`; configure the `production` GitHub environment and its OIDC identity
before enabling deployments.
