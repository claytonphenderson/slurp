az eventhubs namespace create \
    --resource-group slurp \
    --name slurphub \
    --location centralus \
    --sku Standard

az eventhubs eventhub create \
    --resource-group slurp \          
    --namespace-name slurphub \           
    --name events-parallel \                
    --partition-count 32

## note: event hub needs rbac access to write to blob storage for checkpoint
az storage account create \
    --name slurpcheckpoints \
    --resource-group slurp \
    --location centralus \
    --sku Standard_LRS \
    --kind StorageV2 

az storage container create \
    --name eventscheckpoints \
    --account-name slurpcheckpoints \
    --auth-mode login