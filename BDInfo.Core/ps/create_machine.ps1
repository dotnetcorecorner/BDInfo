az login
az deployment group create --name MyMachineDeployment --resource-group UbuntuRG --template-file template.json --parameters parameters.json


#az group delete UbuntuRG