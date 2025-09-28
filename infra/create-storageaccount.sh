az storage account create \
    --name slurpdl \
    --resource-group slurp \
    --location centralus \
    --sku Standard_LRS \
    --kind StorageV2 \
    --hierarchical-namespace true

az storage container create \
    --name events \
    --account-name slurpdl \
    --auth-mode login