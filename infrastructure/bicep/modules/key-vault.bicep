@description('Key Vault name')
param name string

@description('Location for Key Vault')
param location string

@description('Resource tags')
param tags object

@description('Secrets to create')
param secrets array

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: false
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    accessPolicies: []
    publicNetworkAccess: 'Enabled'
  }
}

resource keyVaultSecrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [for secret in secrets: {
  parent: keyVault
  name: secret.name
  properties: {
    value: secret.value
  }
}]

output id string = keyVault.id
output name string = keyVault.name
output vaultUri string = keyVault.properties.vaultUri
