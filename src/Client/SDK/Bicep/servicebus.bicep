import { ServiceBusQueueOptions, ServiceBusQueueOptionsDefault } from 'types.bicep'

param name string
param location string = resourceGroup().location
param options ServiceBusQueueOptions?
var _options = union(ServiceBusQueueOptionsDefault, options ?? {})
var queueNames = [_options.requestQueue, _options.responseQueue]

resource servicebus 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: name
  location: location
  sku: {
    name: _options.sku
    capacity: _options.skuCapacity
  }

  resource queues 'queues' = [for qName in queueNames: {
    name: qName
    properties: {
      lockDuration: _options.lockDuration
      maxSizeInMegabytes: _options.sizeInMB
    }
  }]
}
