import { NodeOptions, ServiceType, EnvionmentVariableArrayType, GitRepoMountArrayType, FileShareMountArrayType, ServiceBusQueueOptions, ServiceBusQueueOptionsDefault } from 'types.bicep'

targetScope = 'subscription'

param location string = 'southeastasia'

param nodeOptions NodeOptions?
param nodeCount int?
param service ServiceType = 'echo'
param environmentVariables EnvionmentVariableArrayType = []
param gitRepos GitRepoMountArrayType = []
param fileShares FileShareMountArrayType = []

param messagingRgName string
param computingRgName string
param appInsightsName string = 'appinsights-${uniqueString(messagingRgName)}'
param serviceBusName string = 'servicebus-${uniqueString(messagingRgName)}'

param serviceBusQueueOptions ServiceBusQueueOptions?
var _serviceBusQueueOptions = union(ServiceBusQueueOptionsDefault, serviceBusQueueOptions ?? {})

var queueOptions = {
  queueType: 'servicebus'
  requestQueue: _serviceBusQueueOptions.requestQueue
  responseQueue: _serviceBusQueueOptions.responseQueue
}

resource messagingRg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: messagingRgName
  location: location
  tags: {
    QueueType: queueOptions.queueType
    RequestQueueName: queueOptions.requestQueue
    ResponseQueueName: queueOptions.responseQueue
  }
}

resource computingRg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: computingRgName
  location: location
  tags: {
    Service: service
  }
}

module servicebus 'servicebus.bicep' = {
  scope: messagingRg
  name: 'servicebus-deployment'
  params: {
    name: serviceBusName
    location: location
    options: _serviceBusQueueOptions
  }
}

module monitor 'appinsights.bicep' = {
  scope: messagingRg
  name: 'monitor-deployment'
  params: {
    name: appInsightsName
    location: location
  }
}

module cluster 'aci-with-assets.bicep' = {
  scope: computingRg
  name: 'cluster-deployment'
  params: {
    nodeOptions: nodeOptions
    count: nodeCount
    service: service
    environmentVariables: environmentVariables
    gitRepos: gitRepos
    fileShares: fileShares
    queueOptions: queueOptions
    serviceBusName: serviceBusName
    serviceBusRg: messagingRgName
    appInsightsName: appInsightsName
    appInsightsRg: messagingRgName
    location: location
  }
  dependsOn: [
    servicebus
    monitor
  ]
}
