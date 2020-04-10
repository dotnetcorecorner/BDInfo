az login
az deployment group create --name ExampleDeployment --resource-group ExampleGroup --template-file template.json --parameters parameters.json