import { ServiceType, EnvionmentVariableArrayType, GitRepoMountArrayType, FileShareMountArrayType } from 'types.bicep'

targetScope = 'subscription'

param service ServiceType = 'echo'
param envionmentVariables EnvionmentVariableArrayType = []
param gitRepos GitRepoMountArrayType = []
param fileShares FileShareMountArrayType = []

param messagingRgName string
param computingRgName string
param location string = 'southeastasia'
param appInsightsName string = 'appinsights-${uniqueString(messagingRgName)}'
param serviceBusName string = 'servicebus-${uniqueString(messagingRgName)}'
param requestQueueName string = 'requests'
param responseQueueName string = 'responses'

var queueOptions = {
  requestQueue: requestQueueName
  responseQueue: responseQueueName
}

resource messagingRg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: messagingRgName
  location: location
}

resource computingRg 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: computingRgName
  location: location
}

module servicebus 'servicebus.bicep' = {
  scope: messagingRg
  name: 'servicebus-deployment'
  params: {
    name: serviceBusName
    location: location
    queueNames: [
      requestQueueName
      responseQueueName
    ]
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
    service: service
    envionmentVariables: envionmentVariables
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
