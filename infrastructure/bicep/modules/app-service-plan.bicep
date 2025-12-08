@description('App Service Plan name')
param name string

@description('Location for App Service Plan')
param location string

@description('SKU name (B1, P1v3, etc.)')
param skuName string

@description('Resource tags')
param tags object

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: name
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: skuName
  }
  properties: {
    reserved: true // Required for Linux
  }
}

output id string = appServicePlan.id
output name string = appServicePlan.name
