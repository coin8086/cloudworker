@export()
type QueueType = 'servicebus' | 'storage'

@export()
type ServiceType = 'cgi' | 'echo' | 'grpc'

@export()
type EnvionmentVariableType = {
  name: string
  value: string?
  secureValue: string?
}

@export()
type EnvionmentVariableArrayType = EnvionmentVariableType[]
