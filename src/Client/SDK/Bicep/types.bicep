@export()
type QueueType = 'servicebus' | 'storage'

//NOTE: connectionString is requried.
@export()
type QueueOptionsType = {
  @secure()
  connectionString: string?
  queueType: QueueType?
  requestQueue: string?
  responseQueue: string?
  messageLease: int?
  queryInterval: int?
}

@export()
var QueueOptionsDefault = {
  queueType: 'servicebus'
  requestQueue: 'requests'
  responseQueue: 'responses'
  messageLease: 60
  queryInterval: 500
}

@export()
type ServiceType = 'cgi' | 'echo' | 'grpc'

//NOTE: Either value or secureValue is requried.
@export()
type EnvionmentVariableType = {
  name: string
  value: string?
  @secure()
  secureValue: string?
}

@export()
type EnvionmentVariableArrayType = EnvionmentVariableType[]

//NOTE: Either repository or privateRepository is requried.
@export()
type GitRepoMountType = {
  name: string
  mountPath: string
  repository: string?
  @secure()
  privateRepository: string?
  directory: string?
  revision: string?
}

@export()
type GitRepoMountArrayType = GitRepoMountType[]

@export()
type FileShareMountType = {
  name: string
  mountPath: string
  fileShareName: string
  storageAccountName: string
  @secure()
  storageAccountKey: string
}

@export()
type FileShareMountArrayType = FileShareMountType[]

@export()
type NodeOptions = {
  cpuCount: int?
  memInGB: int?
  image: string?
}

@export()
var NodeOptionsDefault = {
  cpuCount: 1
  memInGB: 1
  image: 'leizacrdev.azurecr.io/soa/servicehost:1.5-ubuntu22'
}

@export()
type ServiceBusQueueSku = 'Basic' | 'Premium' | 'Standard'

@export()
type ServiceBusSkuBaseCapacity = 1 | 2 | 4 | 8 | 16

@export()
type ServiceBusQueueOptions = {
  sku: ServiceBusQueueSku?
  skuCapacity: int?
  sizeInMB: int?
  lockDuration: string?
  requestQueue: string?
  responseQueue: string?
}

@export()
var ServiceBusQueueOptionsDefault = {
  sku: 'Standard'
  skuCapacity: null
  sizeInMB: 2048
  lockDuration: 'PT1M' //Should be equal with QueueOptionsDefault.messageLease
  requestQueue: QueueOptionsDefault.requestQueue
  responseQueue: QueueOptionsDefault.responseQueue
}
