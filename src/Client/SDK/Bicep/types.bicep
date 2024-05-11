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
type NodeConfig = {
  cpuCount: int?
  memInGB: int?
  image: string?
}

@export()
var NodeConfigDefault = {
  cpuCount: 1
  memInGB: 1
  image: 'leizacrdev.azurecr.io/soa/servicehost:1.5-ubuntu22'
}
