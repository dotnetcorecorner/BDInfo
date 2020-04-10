az login
az deployment group create --name ExampleDeployment --resource-group UbuntuRG --template-file template.json --parameters parameters.json