param name string
param location string = resourceGroup().location

var logSpaceName = '${name}-logspace'

resource logSpace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logSpaceName
  location: location
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    IngestionMode: 'LogAnalytics'
    WorkspaceResourceId: logSpace.id
  }
}
