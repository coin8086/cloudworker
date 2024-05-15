import { NodeOptions, ServiceType, QueueOptionsType, EnvionmentVariableArrayType, GitRepoMountArrayType, FileShareMountArrayType } from 'types.bicep'

param nodeOptions NodeOptions?
param count int = 10
param offset int = 0
param location string = resourceGroup().location
param concurrency int = 100
param service ServiceType = 'echo'
param environmentVariables EnvionmentVariableArrayType = []

param serviceBusName string
param serviceBusRg string
param queueOptions QueueOptionsType?

param appInsightsName string = ''
param appInsightsRg string = ''

param gitRepos GitRepoMountArrayType = []
param fileShares FileShareMountArrayType = []

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
    nodeOptions: nodeOptions
    count: count
    offset: offset
    location: location
    queueOptions: _queueOptions
    appInsights: appInsightsConnectionString
    concurrency: concurrency
    service: service
    environmentVariables: environmentVariables
    gitRepos: gitRepos
    fileShares: fileShares
  }
}
