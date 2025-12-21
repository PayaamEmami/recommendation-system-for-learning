@description('Container Job name')
param name string

@description('Location for Container Job')
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

@description('Cron schedule expression (e.g., "0 0 * * *" for daily at midnight)')
param cronExpression string

@description('Replica timeout in seconds (default: 2 hours)')
param replicaTimeout int = 7200

@description('Replica retry limit (default: 0 for no retries)')
param replicaRetryLimit int = 0

@description('Command to run in the container (optional)')
param command array = []

@description('Arguments to pass to the container (optional)')
param args array = []

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: containerRegistryName
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' existing = {
  name: keyVaultName
}

resource containerJob 'Microsoft.App/jobs@2024-03-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: environmentId
    configuration: {
      scheduleTriggerConfig: {
        cronExpression: cronExpression
        parallelism: 1
        replicaCompletionCount: 1
      }
      replicaTimeout: replicaTimeout
      replicaRetryLimit: replicaRetryLimit
      triggerType: 'Schedule'
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
    }
    template: {
      containers: [
        {
          name: name
          // Use public hello-world image initially, will be updated by CI/CD
          image: 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: environmentVariables
          command: empty(command) ? null : command
          args: empty(args) ? null : args
        }
      ]
    }
  }
}

// Grant Container Job access to Key Vault
resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-02-01' = {
  name: 'add'
  parent: keyVault
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: containerJob.identity.principalId
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

output id string = containerJob.id
output name string = containerJob.name
