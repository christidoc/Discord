# GitHub Actions deployment to Azure Container Apps

The workflow in `.github/workflows/deploy.yml` tests and builds on pull requests
to `main`. A push or manual run on `main` deploys the bot as a continuously
running Azure Container App. It has no ingress, keeps exactly one replica, and
uses a user-assigned managed identity to pull its image from Azure Container
Registry (ACR).

The Bicep templates create the ACR, Log Analytics workspace, Container Apps
environment, image-pull identity and its `AcrPull` assignment, then the app.
The resource group is a one-time deployment boundary created during setup; all
application resources are managed by the templates. The bootstrap template runs
first so a brand-new registry exists before the workflow pushes the image.

## One-time Azure setup

1. Create a **dedicated resource group** in the Azure portal in the region you
   want to use. Record its name, subscription ID, and region. The GitHub
   deployment identity and the ACR should be scoped to this group.
2. Choose a globally unique ACR name using only lowercase letters and numbers
   (5–50 characters).
3. In Microsoft Entra ID, create an app registration and its service principal.
   Add a **federated credential** for GitHub Actions:
   - Organization and repository: your GitHub `OWNER/REPOSITORY`
   - Entity type: **Environment**
   - Environment: `production`
   - Issuer: `https://token.actions.githubusercontent.com`
   - Audience: `api://AzureADTokenExchange`

   Do not create a client secret. The workflow authenticates with OIDC.
4. Grant that service principal these Azure roles, scoped to the dedicated
   resource group:
   - **Contributor** — deploy/update resources from Bicep.
   - **Role Based Access Control Administrator** — create the narrowly scoped
     `AcrPull` assignment for the Container App identity.
   - **AcrPush** — push images to registries in this dedicated group. Because
     the registry is created by the workflow, this role must initially be
     granted at resource-group scope.

   These permissions are needed for the template-driven first deployment. Use
   a dedicated resource group; do not grant these roles at subscription scope.
   The registry itself has its admin account disabled.
5. In GitHub, create the `production` environment. Add these **environment
   variables** (Settings → Environments → `production` → Environment variables):
   - `AZURE_CLIENT_ID` — app registration's Application (client) ID.
   - `AZURE_TENANT_ID` — Microsoft Entra tenant ID.
   - `AZURE_SUBSCRIPTION_ID` — subscription ID containing the resource group.
   - `AZURE_RESOURCE_GROUP` — pre-created resource group name.
   - `AZURE_LOCATION` — Azure region (for example `westeurope`); use the same
     region as the resource group.
   - `AZURE_ACR_NAME` — globally unique ACR name selected above.
6. Add `DISCORD_TOKEN` as a **GitHub environment secret** for `production`.
   Revoke and replace any token previously exposed in a development settings
   file before saving the replacement here. Never put it in Bicep parameters
   files, GitHub variables, the Docker image, or source control.
7. Protect `main` and configure required reviewers for the `production`
   environment if you want a human approval before each deployment. Pull
   requests only test/build; they do not have Azure credentials or deploy.

## Deployment behavior

For each deployment, the workflow applies `infra/bootstrap.bicep`, builds and
pushes an image tagged with the full Git commit SHA, then applies
`infra/main.bicep`. `discordToken` is a Bicep secure parameter; the app stores it
as a Container Apps secret and exposes it to the process only as
`DISCORD_TOKEN`. It is not written to a parameter file or emitted as a template
output. The workflow uses `--output none` for the ARM deployment commands.

The app intentionally has no ingress, HTTP endpoint, or scale-to-zero. It
reserves one active replica so it can maintain its Discord gateway connection.
Azure Container Apps and Log Analytics can incur charges; review regional
pricing before the first run. Logs are available in the Container App and its
Log Analytics workspace.

To view the deployed worker's logs:

```bash
az containerapp logs show \
  --name mydiscordbot \
  --resource-group "$AZURE_RESOURCE_GROUP" \
  --follow
```

Do not run a deployment until the Azure identity, role assignments, GitHub
environment variables, and replacement bot token have been configured.
