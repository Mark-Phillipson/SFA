# Publish Admin

This document contains the required GitHub Actions secrets and instructions for deploying the SFA_WebAPI backend to Azure, as well as the publish command.

## Required GitHub Secrets

Add these secrets to your repository in GitHub:

```yaml
AZURE_WEBAPP_NAME: SFA-WebAPI
AZURE_WEBAPP_PUBLISH_PROFILE: <Paste the XML content from Azure Portal: Web App > Get publish profile>
AZURE_RESOURCE_GROUP: SFA-Prod-RG
OPENAI_API_KEY: <Your OpenAI API Key>
```

## How to Add GitHub Secrets

1. Go to your repository on GitHub.
2. Click **Settings** > **Secrets and variables** > **Actions**.
3. Click **New repository secret**.
4. Add each secret above, using the name and value as shown.
   - For `AZURE_WEBAPP_PUBLISH_PROFILE`, copy the full XML from Azure Portal > App Service > Get publish profile.
   - For `OPENAI_API_KEY`, use your actual OpenAI API key.

## Publish Command

Use this command in PowerShell to build and publish your backend app:

```pwsh
dotnet publish .\SFA_WebAPI\SFA_WebAPI.csproj -c Release -o .\publish
```

- This will create a `publish` folder with the build output, ready for deployment.

## Deployment

- You can deploy the contents of the `publish` folder using the Azure App Service extension in VS Code, the Azure Portal, or via GitHub Actions (see `.github/workflows/deploy-backend.yml`).

---

**Tip:** Make sure your Azure resources (Web App, Resource Group) exist before adding secrets.
