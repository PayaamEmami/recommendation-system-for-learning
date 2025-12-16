// Main Bicep template for RSL infrastructure using Container Apps
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

@description('OpenAI API key (for direct OpenAI API access)')
@secure()
param openAIApiKey string

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

// Container Registry
module containerRegistry 'modules/container-registry.bicep' = {
  name: 'containerRegistryDeployment'
  params: {
    name: '${appName}${environment}${uniqueSuffix}'
    location: location
    tags: tags
  }
}

// SQL Server
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

// Key Vault
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
        name: 'OpenAIApiKey'
        value: openAIApiKey
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

// Application Insights
module applicationInsights 'modules/application-insights.bicep' = {
  name: 'applicationInsightsDeployment'
  params: {
    name: '${resourcePrefix}-ai'
    location: location
    tags: tags
  }
}

// Log Analytics Workspace (for Container Apps)
module logAnalytics 'modules/log-analytics.bicep' = {
  name: 'logAnalyticsDeployment'
  params: {
    name: '${resourcePrefix}-logs'
    location: location
    tags: tags
  }
}

// Container Apps Environment
module containerAppsEnvironment 'modules/container-apps-environment.bicep' = {
  name: 'containerAppsEnvironmentDeployment'
  params: {
    name: '${resourcePrefix}-env'
    location: location
    logAnalyticsWorkspaceId: logAnalytics.outputs.id
    tags: tags
  }
}

// API Container App
module apiApp 'modules/container-app.bicep' = {
  name: 'apiAppDeployment'
  params: {
    name: '${resourcePrefix}-api'
    location: location
    environmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'rsl-api'
    imageTag: 'latest'
    keyVaultName: keyVault.outputs.name
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    minReplicas: 1
    maxReplicas: 3
    secrets: [
      {
        name: 'sql-connection-string'
        value: 'Server=tcp:${sqlServer.outputs.serverFqdn},1433;Initial Catalog=${appName}-db;Persist Security Info=False;User ID=${sqlAdminUsername};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
      }
      {
        name: 'azure-openai-api-key'
        value: azureOpenAIApiKey
      }
      {
        name: 'azure-search-api-key'
        value: azureSearchApiKey
      }
      {
        name: 'jwt-secret-key'
        value: jwtSecretKey
      }
    ]
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'ConnectionStrings__DefaultConnection'
        secretRef: 'sql-connection-string'
      }
      {
        name: 'Embedding__UseAzure'
        value: 'false'
      }
      {
        name: 'Embedding__ApiKey'
        secretRef: 'openai-api-key'
      }
      {
        name: 'Embedding__ModelName'
        value: 'text-embedding-3-small'
      }
      {
        name: 'Embedding__Dimensions'
        value: '1536'
      }
      {
        name: 'Embedding__MaxBatchSize'
        value: '100'
      }
      {
        name: 'AzureAISearch__Endpoint'
        value: azureSearchEndpoint
      }
      {
        name: 'AzureAISearch__ApiKey'
        secretRef: 'azure-search-api-key'
      }
      {
        name: 'AzureAISearch__IndexName'
        value: 'rsl-resources'
      }
      {
        name: 'AzureAISearch__EmbeddingDimensions'
        value: '1536'
      }
      {
        name: 'JwtSettings__SecretKey'
        secretRef: 'jwt-secret-key'
      }
      {
        name: 'JwtSettings__Issuer'
        value: 'https://${resourcePrefix}-api.${containerAppsEnvironment.outputs.defaultDomain}'
      }
      {
        name: 'JwtSettings__Audience'
        value: 'https://${resourcePrefix}-web.${containerAppsEnvironment.outputs.defaultDomain}'
      }
      {
        name: 'JwtSettings__ExpirationMinutes'
        value: '60'
      }
      {
        name: 'Cors__AllowedOrigins__0'
        value: 'https://${resourcePrefix}-web.${containerAppsEnvironment.outputs.defaultDomain}'
      }
      {
        name: 'Registration__Enabled'
        value: 'false'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsights.outputs.connectionString
      }
    ]
    tags: tags
  }
}

// Web Container App
module webApp 'modules/container-app.bicep' = {
  name: 'webAppDeployment'
  params: {
    name: '${resourcePrefix}-web'
    location: location
    environmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'rsl-web'
    imageTag: 'latest'
    keyVaultName: keyVault.outputs.name
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    minReplicas: 0
    maxReplicas: 3
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'ApiBaseUrl'
        value: apiApp.outputs.url
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsights.outputs.connectionString
      }
    ]
    tags: tags
  }
}

// Jobs Container App
module jobsApp 'modules/container-app.bicep' = {
  name: 'jobsAppDeployment'
  params: {
    name: '${resourcePrefix}-jobs'
    location: location
    environmentId: containerAppsEnvironment.outputs.id
    containerRegistryName: containerRegistry.outputs.name
    imageName: 'rsl-jobs'
    imageTag: 'latest'
    keyVaultName: keyVault.outputs.name
    applicationInsightsConnectionString: applicationInsights.outputs.connectionString
    minReplicas: 1
    maxReplicas: 1
    secrets: [
      {
        name: 'sql-connection-string'
        value: 'Server=tcp:${sqlServer.outputs.serverFqdn},1433;Initial Catalog=${appName}-db;Persist Security Info=False;User ID=${sqlAdminUsername};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
      }
      {
        name: 'azure-openai-api-key'
        value: azureOpenAIApiKey
      }
      {
        name: 'azure-search-api-key'
        value: azureSearchApiKey
      }
    ]
    environmentVariables: [
      {
        name: 'ASPNETCORE_ENVIRONMENT'
        value: 'Production'
      }
      {
        name: 'ConnectionStrings__DefaultConnection'
        secretRef: 'sql-connection-string'
      }
      {
        name: 'Embedding__UseAzure'
        value: 'false'
      }
      {
        name: 'Embedding__ApiKey'
        secretRef: 'openai-api-key'
      }
      {
        name: 'Embedding__ModelName'
        value: 'text-embedding-3-small'
      }
      {
        name: 'Embedding__Dimensions'
        value: '1536'
      }
      {
        name: 'Embedding__MaxBatchSize'
        value: '100'
      }
      {
        name: 'AzureAISearch__Endpoint'
        value: azureSearchEndpoint
      }
      {
        name: 'AzureAISearch__ApiKey'
        secretRef: 'azure-search-api-key'
      }
      {
        name: 'AzureAISearch__IndexName'
        value: 'rsl-resources'
      }
      {
        name: 'AzureAISearch__EmbeddingDimensions'
        value: '1536'
      }
      {
        name: 'OpenAI__UseAzure'
        value: 'false'
      }
      {
        name: 'OpenAI__ApiKey'
        secretRef: 'openai-api-key'
      }
      {
        name: 'OpenAI__Model'
        value: 'gpt-5-nano'
      }
      {
        name: 'OpenAI__MaxTokens'
        value: '4096'
      }
      {
        name: 'Jobs__RunOnStartup'
        value: 'true'
      }
      {
        name: 'JobSchedules__SourceIngestionCron'
        value: '0 0 * * *'
      }
      {
        name: 'JobSchedules__DailyFeedGenerationCron'
        value: '0 2 * * *'
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsights.outputs.connectionString
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
output containerAppsEnvironmentName string = containerAppsEnvironment.outputs.name
output apiAppUrl string = apiApp.outputs.url
output webAppUrl string = webApp.outputs.url
output jobsAppUrl string = jobsApp.outputs.url
output applicationInsightsConnectionString string = applicationInsights.outputs.connectionString
