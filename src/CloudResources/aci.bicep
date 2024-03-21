/*
 * Container
 */

param prefix string = 'servicehost'
param count int = 1
param offset int = 0
param location string = 'southeastasia'
param cpu int = 1
param memoryInGB int = 1

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
param messageLease int = 60
param queryInterval int = 500

/*
 * Service
 */

@allowed([
  'cgi'
  'echo'
])
param service string = 'echo'
param cgiFileName string = '/bin/bash'
param cgiArguments string = '-'

/*
 * Worker
 */
param concurrency int = 20

/*
 * Monitor
 */

@secure()
param appInsights string = ''

var serviceMap = {
  echo: '/services/echoservice/Cloud.Soa.EchoService.dll'
  cgi: '/services/cgiservice/Cloud.Soa.CGIService.dll'
}
var assemblyPath = serviceMap[service]

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
            image: 'leizacrdev.azurecr.io/soa/servicehost:1.0-ubuntu22'
            environmentVariables: [
              {
                name: 'Worker__Concurrency'
                value: '${concurrency}'
              }
              {
                name: 'Queues__QueueType'
                value: queueType
              }
              {
                name: 'Queues__ConnectionString'
                secureValue: connectionString
              }
              {
                name: 'Queues__MessageLease'
                value: '${messageLease}'
              }
              {
                name: 'Queues__QueryInterval'
                value: '${queryInterval}'
              }
              {
                name: 'Service__AssemblyPath'
                value: assemblyPath
              }
              {
                name: 'CGI_FileName'
                value: cgiFileName
              }
              {
                name: 'CGI_Arguments'
                value: cgiArguments
              }
              {
                name: 'ApplicationInsights__ConnectionString'
                secureValue: appInsights
              }
            ]
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
