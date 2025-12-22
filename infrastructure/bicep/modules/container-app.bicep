@description('Container App name')
param name string

@description('Location for Container App')
param location string

@description('Container Apps Environment ID')
param environmentId string

@description('Container Registry name')
param containerRegistryName string

@description('Container image name')
param imageName string

@description('Container image tag')
param imageTag string

@description('Key Vault name for secrets')
param keyVaultName string

@description('Application Insights connection string')
param applicationInsightsConnectionString string

@description('Environment variables')
param environmentVariables array

@description('Secrets configuration - array of objects with {name, value}')
param secrets array = []

@description('Resource tags')
param tags object

@description('Minimum replicas')
param minReplicas int = 0

@description('Maximum replicas')
param maxReplicas int = 3

@description('Enable ingress (HTTP/HTTPS endpoints)')
param enableIngress bool = true

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    managedEnvironmentId: environmentId
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: concat([
        {
          name: 'registry-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ], secrets)
      ingress: enableIngress ? {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      } : null
    }
    template: {
      containers: [
        {
          name: name
          image: '${containerRegistry.properties.loginServer}/${imageName}:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: environmentVariables
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

// Grant Container App access to Key Vault
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-02-01' = {
  name: 'add'
  parent: keyVault
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: containerApp.identity.principalId
        permissions: {
          secrets: [
            'get'
            'list'
          ]
        }
      }
    ]
  }
}

output id string = containerApp.id
output name string = containerApp.name
output fqdn string = enableIngress ? containerApp.properties.configuration.ingress.fqdn : ''
output url string = enableIngress ? 'https://${containerApp.properties.configuration.ingress.fqdn}' : ''
