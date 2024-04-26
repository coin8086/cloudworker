import { ServiceType, QueueOptionsType, QueueOptionsDefault, EnvionmentVariableArrayType, GitRepoMountArrayType, FileShareMountArrayType } from 'types.bicep'

/*
 * Container
 */

param prefix string = 'servicehost'
param count int = 1
param offset int = 0
param location string = 'southeastasia'
param cpu int = 1
param memoryInGB int = 1
param image string = 'leizacrdev.azurecr.io/soa/servicehost:1.5-ubuntu22'

/*
 * Queue
 */

param queueOptions QueueOptionsType
var _queueOptions = union(QueueOptionsDefault, queueOptions)

/*
 * Monitor
 */

 @secure()
 param appInsights string = ''

/*
 * Worker
 */
param concurrency int = 20

/*
 * Service
 */

param service ServiceType = 'echo'
param envionmentVariables EnvionmentVariableArrayType = []

var serviceMap = {
  cgi: '/services/cgi/CloudWorker.Services.CGI.dll'
  echo: '/services/echo/CloudWorker.Services.Echo.dll'
  grpc: '/services/grpc/CloudWorker.Services.GRpc.dll'
}
var assemblyPath = serviceMap[service]

var coreEnvVars = [
  {
    name: 'Queues__QueueType'
    value: _queueOptions.queueType
  }
  {
    name: 'Queues__ConnectionString'
    secureValue: _queueOptions.connectionString
  }
  {
    name: 'Queues__Request__QueueName'
    value: _queueOptions.requestQueue
  }
  {
    name: 'Queues__Response__QueueName'
    value: _queueOptions.responseQueue
  }
  {
    name: 'Queues__MessageLease'
    value: _queueOptions.messageLease
  }
  {
    name: 'Queues__QueryInterval'
    value: _queueOptions.queryInterval
  }
  {
    name: 'ApplicationInsights__ConnectionString'
    value: appInsights
  }
  {
    name: 'Worker__Concurrency'
    value: concurrency
  }
  {
    name: 'Service__AssemblyPath'
    value: assemblyPath
  }
]

var coreEnvVarsAsObj = toObject(coreEnvVars, e => e.name)
var envionmentVariablesAsObj = toObject(envionmentVariables, e => e.name)
var envVarsAsObj = union(coreEnvVarsAsObj, envionmentVariablesAsObj)
var envVars = map(items(envVarsAsObj), item => item.value)

/*
 * Mounts
 */

param gitRepos GitRepoMountArrayType = []
var gitRepoMounts = map(gitRepos, e => { name: e.name, mountPath: e.mountPath })
var gitRepoVolumes = map(gitRepos,
  e => {
    name: e.name
    gitRepo: {
      repository: (e.?privateRepository ?? e.repository)!
      directory: e.?directory
      revision: e.?revision
    }
  })

param fileShares FileShareMountArrayType = []
var fileShareMounts = map(fileShares, e => { name: e.name, mountPath: e.mountPath })
var fileShareVolumes = map(fileShares,
  e => {
    name: e.name
    azureFile: {
      shareName: e.fileShareName
      storageAccountName: e.storageAccountName
      storageAccountKey: e.storageAccountKey
    }
  })

var volumeMounts = concat(gitRepoMounts, fileShareMounts)
var volumes = concat(gitRepoVolumes, fileShareVolumes)

resource containers 'Microsoft.ContainerInstance/containerGroups@2023-05-01' = [
  for i in range(0, count): {
    name: '${prefix}${(i+offset)}'
    location: location
    properties: {
      sku: 'Standard'
      containers: [
        {
          name: 'servicehost'
          properties: {
            image: image
            environmentVariables: envVars
            resources: {
              requests: {
                cpu: cpu
                memoryInGB: memoryInGB
              }
            }
            volumeMounts: volumeMounts
          }
        }
      ]
      initContainers: []
      restartPolicy: 'Always'
      osType: 'Linux'
      volumes: volumes
    }
  }
]
