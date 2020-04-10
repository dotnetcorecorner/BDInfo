az login

$myResourceGroup="UbuntuRG"
$location="westus"
$myVm="UbuntuVM"
$image="UbuntuLTS"
$user="tmm2012"
$password="Automatica_2008^"
$myDataDisk="UbuntuDisk"
$dns="myubuntyvm112"

az group create --name $myResourceGroup --location $location
az vm create --resource-group $myResourceGroup --name $myVM image $image --admin-username $user --admin-password $password --authentication-type password --os-type linux --public-ip-address-allocation static --public-ip-address-dns-name $dns --size Standard_DS2_v2 --nsg-rule SSH

az vm disk attach -g $myResourceGroup --vm-name $myVM --name $myDataDisk --new --size-gb 250

# https://docs.microsoft.com/en-us/azure/virtual-machines/linux/add-disk
#az group delete --name $myResourceGroup