param count int = 10
param offset int = 0
param concurrency int = 100
param service string = 'cgi'
param location string = resourceGroup().location

param serviceBusName string
param serviceBusRg string
param requestQueueName string = 'requests'
param responseQueueName string = 'responses'

param appInsightsName string = ''
param appInsightsRg string = ''

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusName
  scope: resourceGroup(serviceBusRg)
}

var serviceBusEndpoint = '${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey'
var serviceBusConnectionString = listKeys(serviceBusEndpoint, serviceBus.apiVersion).primaryConnectionString

var useMonitor = !empty(appInsightsName) && !empty(appInsightsRg)

resource monitor 'Microsoft.Insights/components@2020-02-02' existing = if (useMonitor) {
  name: appInsightsName
  scope: resourceGroup(appInsightsRg)
}

var appInsightsConnectionString = useMonitor ? monitor.properties.ConnectionString : ''

module aci 'aci.bicep' = {
  name: 'aci-deployment'
  params: {
    count: count
    offset: offset
    concurrency: concurrency
    service: service
    location: location
    connectionString: serviceBusConnectionString
    requestQueue: requestQueueName
    responseQueue: responseQueueName
    appInsights: appInsightsConnectionString
  }
}
