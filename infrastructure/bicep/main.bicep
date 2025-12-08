// Main Bicep template for RSL infrastructure
targetScope = 'resourceGroup'

@description('Application name prefix')
param appName string = 'rsl'

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environment string = 'dev'

@description('Azure region for resources')
param location string = resourceGroup().location

@description('SQL Server administrator username')
@secure()
param sqlAdminUsername string

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

@description('Azure OpenAI endpoint')
param azureOpenAIEndpoint string

@description('Azure OpenAI API key')
@secure()
param azureOpenAIApiKey string

@description('Azure OpenAI embedding deployment name')
param azureOpenAIEmbeddingDeployment string = 'text-embedding-3-small'

@description('Azure OpenAI chat deployment name')
param azureOpenAIChatDeployment string = 'gpt-4o'

@description('Azure AI Search endpoint')
param azureSearchEndpoint string

@description('Azure AI Search API key')
@secure()
param azureSearchApiKey string

@description('JWT secret key for authentication')
@secure()
param jwtSecretKey string

@description('Tags for all resources')
param tags object = {
  Application: 'RSL'
  Environment: environment
  ManagedBy: 'Bicep'
}

// Variables
var uniqueSuffix = uniqueString(resourceGroup().id)
var resourcePrefix = '${appName}-${environment}'

// Modules
module containerRegistry 'modules/container-registry.bicep' = {
  name: 'containerRegistryDeployment'
  params: {
    name: '${appName}${environment}${uniqueSuffix}'
    location: location
    tags: tags
  }
}

module sqlServer 'modules/sql-server.bicep' = {
  name: 'sqlServerDeployment'
  params: {
    serverName: '${resourcePrefix}-sql-${uniqueSuffix}'
    location: location
    administratorLogin: sqlAdminUsername
    administratorPassword: sqlAdminPassword
    databaseName: '${appName}-db'
    tags: tags
  }
}

module appServicePlan 'modules/app-service-plan.bicep' = {
  name: 'appServicePlanDeployment'
  params: {
    name: '${resourcePrefix}-plan'
    location: location
    skuName: environment == 'prod' ? 'P1v3' : 'B1'
    tags: tags
  }
}

module keyVault 'modules/key-vault.bicep' = {
  name: 'keyVaultDeployment'
  params: {
    name: '${resourcePrefix}-kv-${uniqueSuffix}'
    location: location
    tags: tags
    secrets: [
      {
        name: 'SqlConnectionString'
        value: 'Server=tcp:${sqlServer.outputs.serverFqdn},1433;Initial Catalog=${appName}-db;Persist Security Info=False;User ID=${sqlAdminUsername};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
      }
      {
        name: 'AzureOpenAIApiKey'
        value: azureOpenAIApiKey
      }
      {
        name: 'AzureSearchApiKey'
        value: azureSearchApiKey
      }
      {
        name: 'JwtSecretKey'
        value: jwtSecretKey
      }
    ]
  }
}

module applicationInsights 'modules/application-insights.bicep' = {
  name: 'applicationInsightsDeployment'
  params: {
    name: '${resourcePrefix}-ai'
    location: location
    tags: tags
  }
}

module apiApp 'modules/app-service.bicep' = {
  name: 'apiAppDeployment'
  params: {
    name: '${resourcePrefix}-api'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'rsl-api'
    imageTag: 'latest'
    keyVaultName: keyVault.outputs.name
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    appSettings: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'SQL_CONNECTION_STRING'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/SqlConnectionString/)'
      }
      {
        name: 'AZURE_OPENAI_ENDPOINT'
        value: azureOpenAIEndpoint
      }
      {
        name: 'AZURE_OPENAI_API_KEY'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/AzureOpenAIApiKey/)'
      }
      {
        name: 'AZURE_OPENAI_EMBEDDING_DEPLOYMENT'
        value: azureOpenAIEmbeddingDeployment
      }
      {
        name: 'AZURE_SEARCH_ENDPOINT'
        value: azureSearchEndpoint
      }
      {
        name: 'AZURE_SEARCH_API_KEY'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/AzureSearchApiKey/)'
      }
      {
        name: 'JWT_SECRET_KEY'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/JwtSecretKey/)'
      }
      {
        name: 'JWT_ISSUER'
        value: 'https://${resourcePrefix}-api.azurewebsites.net'
      }
      {
        name: 'JWT_AUDIENCE'
        value: 'https://${resourcePrefix}-web.azurewebsites.net'
      }
      {
        name: 'CORS_ALLOWED_ORIGINS'
        value: 'https://${resourcePrefix}-web.azurewebsites.net'
      }
    ]
    tags: tags
  }
}

module webApp 'modules/app-service.bicep' = {
  name: 'webAppDeployment'
  params: {
    name: '${resourcePrefix}-web'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'rsl-web'
    imageTag: 'latest'
    keyVaultName: keyVault.outputs.name
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    appSettings: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'API_BASE_URL'
        value: 'https://${resourcePrefix}-api.azurewebsites.net'
      }
    ]
    tags: tags
  }
}

module jobsApp 'modules/app-service.bicep' = {
  name: 'jobsAppDeployment'
  params: {
    name: '${resourcePrefix}-jobs'
    location: location
    appServicePlanId: appServicePlan.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'rsl-jobs'
    imageTag: 'latest'
    keyVaultName: keyVault.outputs.name
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    appSettings: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'SQL_CONNECTION_STRING'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/SqlConnectionString/)'
      }
      {
        name: 'AZURE_OPENAI_ENDPOINT'
        value: azureOpenAIEndpoint
      }
      {
        name: 'AZURE_OPENAI_API_KEY'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/AzureOpenAIApiKey/)'
      }
      {
        name: 'AZURE_OPENAI_EMBEDDING_DEPLOYMENT'
        value: azureOpenAIEmbeddingDeployment
      }
      {
        name: 'AZURE_OPENAI_CHAT_DEPLOYMENT'
        value: azureOpenAIChatDeployment
      }
      {
        name: 'AZURE_SEARCH_ENDPOINT'
        value: azureSearchEndpoint
      }
      {
        name: 'AZURE_SEARCH_API_KEY'
        value: '@Microsoft.KeyVault(SecretUri=${keyVault.outputs.vaultUri}secrets/AzureSearchApiKey/)'
      }
    ]
    tags: tags
  }
}

// Outputs
output containerRegistryName string = containerRegistry.outputs.name
output containerRegistryLoginServer string = containerRegistry.outputs.loginServer
output sqlServerFqdn string = sqlServer.outputs.serverFqdn
output keyVaultName string = keyVault.outputs.name
output apiAppUrl string = 'https://${resourcePrefix}-api.azurewebsites.net'
output webAppUrl string = 'https://${resourcePrefix}-web.azurewebsites.net'
output applicationInsightsConnectionString string = applicationInsights.outputs.connectionString
