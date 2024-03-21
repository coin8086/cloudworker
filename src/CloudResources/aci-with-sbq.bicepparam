using './aci-with-sbq.bicep'

param count = 10
param offset = 0
param concurrency = 100
param service = 'cgi'
param serviceBusName = ''
param serviceBusRg = ''
param appInsightsName = ''
param appInsightsRg = ''
