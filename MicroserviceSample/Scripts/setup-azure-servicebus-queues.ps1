# setup-azure-servicebus-queues.ps1
# Creates the required Service Bus queues for Azure Service Bus Basic tier.
# Basic tier cannot create queues programmatically (AutoDeleteOnIdle not supported).
# Run this script ONCE before starting the application with AzureServiceBus provider.
#
# Usage:
#   .\setup-azure-servicebus-queues.ps1 -ResourceGroup "your-rg" -Namespace "your-namespace"

param(
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroup,

    [Parameter(Mandatory=$true)]
    [string]$Namespace
)

$queues = @(
    "submit-order",
    "process-payment",
    "reserve-inventory",
    "order-completed",
    "order-faulted",
    "order-saga"
)

Write-Host "Creating Azure Service Bus queues in namespace '$Namespace'..." -ForegroundColor Cyan

foreach ($queue in $queues) {
    Write-Host "  Creating queue: $queue" -ForegroundColor Yellow
    az servicebus queue create `
        --resource-group $ResourceGroup `
        --namespace-name $Namespace `
        --name $queue `
        --max-size 1024
}

Write-Host "`nAll queues created successfully!" -ForegroundColor Green
Write-Host "You can now run the application with MessageBroker:Provider = AzureServiceBus"
