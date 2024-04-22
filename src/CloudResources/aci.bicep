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

@allowed([
  'servicebus'
  'storage'
])
param queueType string = 'servicebus'

@secure()
param connectionString string
param requestQueue string = 'requests'
param responseQueue string = 'responses'
param messageLease int = 60
param queryInterval int = 500

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

 @allowed([
  'cgi'
  'echo'
  'grpc'
])
param service string = 'echo'
param envionmentVariables array = []

var serviceMap = {
  cgi: '/services/cgiservice/CloudWorker.Services.CGI.dll'
  echo: '/services/echoservice/CloudWorker.Services.Echo.dll'
  grpc: '/services/grpc/CloudWorker.Services.GRpc.dll'
}
var assemblyPath = serviceMap[service]

var coreEnvVars = [
  {
    name: 'Queues__QueueType'
    value: queueType
  }
  {
    name: 'Queues__ConnectionString'
    secureValue: connectionString
  }
  {
    name: 'Queues__Request__QueueName'
    value: requestQueue
  }
  {
    name: 'Queues__Response__QueueName'
    value: responseQueue
  }
  {
    name: 'Queues__MessageLease'
    value: messageLease
  }
  {
    name: 'Queues__QueryInterval'
    value: queryInterval
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
var envVars = map(items(envVarsAsObj),
  item => contains(item.value, 'secureValue') ? { name: item.key, secureValue: item.value.secureValue } : { name: item.key, value: item.value.value })

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
          }
        }
      ]
      initContainers: []
      restartPolicy: 'Always'
      osType: 'Linux'
    }
  }
]
