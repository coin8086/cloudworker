param name string
param queueNames array = []
param location string = resourceGroup().location

resource servicebus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: name
  location: location
  sku: {
    name: 'Standard'
  }

  resource queues 'queues' = [for qName in queueNames: {
    name: qName
    properties: {
      lockDuration: 'PT1M'
      maxSizeInMegabytes: 2048
    }
  }]
}
