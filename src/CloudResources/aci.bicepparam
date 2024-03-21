using './aci.bicep'

param count = 10
param offset = 0
param concurrency = 100
param service = 'cgi'
param queueType = 'servicebus'

//This parameter is required
param connectionString = ''

//Better to have this
param appInsights = ''
