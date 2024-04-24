@export()
type QueueType = 'servicebus' | 'storage'

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

