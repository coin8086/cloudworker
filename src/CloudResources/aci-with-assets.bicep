import { ServiceType, QueueOptionsType, EnvionmentVariableArrayType } from 'types.bicep'

param count int = 10
param offset int = 0
param concurrency int = 100
param service ServiceType = 'echo'
param envionmentVariables EnvionmentVariableArrayType = []
param location string = resourceGroup().location

param serviceBusName string
param serviceBusRg string
param queueOptions QueueOptionsType?

param appInsightsName string = ''
param appInsightsRg string = ''

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' existing = {
  name: serviceBusName
  scope: resourceGroup(serviceBusRg)
}

var serviceBusEndpoint = '${serviceBus.id}/AuthorizationRules/RootManageSharedAccessKey'
var serviceBusConnectionString = listKeys(serviceBusEndpoint, serviceBus.apiVersion).primaryConnectionString
var _queueOptions = union(queueOptions ?? {}, { connectionString: serviceBusConnectionString })

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
    envionmentVariables: envionmentVariables
    location: location
    queueOptions: _queueOptions
    appInsights: appInsightsConnectionString
  }
}
