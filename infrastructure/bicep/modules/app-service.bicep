@description('App Service name')
param name string

@description('Location for App Service')
param location string

@description('App Service Plan ID')
param appServicePlanId string

@description('Container Registry name')
param containerRegistryName string

@description('Container image name')
param imageName string

@description('Container image tag')
param imageTag string

@description('Key Vault name for managed identity access')
param keyVaultName string

@description('Application Insights connection string')
param applicationInsightsConnectionString string

@description('App settings')
param appSettings array

@description('Resource tags')
param tags object

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' existing = {
  name: containerRegistryName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name: name
  location: location
  tags: tags
  kind: 'app,linux,container'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    clientAffinityEnabled: false
    siteConfig: {
      linuxFxVersion: 'DOCKER|${containerRegistry.properties.loginServer}/${imageName}:${imageTag}'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: concat(appSettings, [
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://${containerRegistry.properties.loginServer}'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_USERNAME'
          value: listCredentials(containerRegistry.id, containerRegistry.apiVersion).username
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_PASSWORD'
          value: listCredentials(containerRegistry.id, containerRegistry.apiVersion).passwords[0].value
        }
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsightsConnectionString
        }
      ])
    }
  }
}

// Grant Key Vault access to the App Service managed identity
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: appService.identity.principalId
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

output id string = appService.id
output name string = appService.name
output defaultHostName string = appService.properties.defaultHostName
output principalId string = appService.identity.principalId
