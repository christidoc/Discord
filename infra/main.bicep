targetScope = 'resourceGroup'

@description('Azure region for the Container App. Must match the bootstrap resources.')
param location string = resourceGroup().location

@minLength(5)
@maxLength(50)
@description('Name of the existing Azure Container Registry.')
param acrName string

@description('Immutable image tag to deploy, normally the Git commit SHA.')
param containerImageTag string

@secure()
@minLength(1)
@description('Discord bot token. Provided by GitHub Actions and stored only as a Container App secret.')
param discordToken string

@description('Name of the existing Container Apps managed environment.')
param environmentName string = 'mydiscordbot-env'

@description('Name of the existing user-assigned image-pull identity.')
param pullIdentityName string = 'mydiscordbot-pull'

@description('Name of the Container App.')
param containerAppName string = 'mydiscordbot'

@description('Image repository in Azure Container Registry.')
param imageRepository string = 'mydiscordbot'

resource registry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: acrName
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: environmentName
}

resource pullIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  name: pullIdentityName
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: containerAppName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${pullIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      activeRevisionsMode: 'Single'
      secrets: [
        {
          name: 'discord-token'
          value: discordToken
        }
      ]
      registries: [
        {
          server: registry.properties.loginServer
          identity: pullIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'discord-bot'
          image: '${registry.properties.loginServer}/${imageRepository}:${containerImageTag}'
          resources: {
            cpu: 0.25
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'DISCORD_TOKEN'
              secretRef: 'discord-token'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

output containerAppName string = containerApp.name
output image string = '${registry.properties.loginServer}/${imageRepository}:${containerImageTag}'
