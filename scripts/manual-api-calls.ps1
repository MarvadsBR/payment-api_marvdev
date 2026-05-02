param(
    [string]$BaseUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

function Invoke-Api {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Method,

        [Parameter(Mandatory = $true)]
        [string]$Path,

        [object]$Body
    )

    $url = "$BaseUrl$Path"

    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Compress
        return curl.exe -s -X $Method $url -H "Content-Type: application/json" --data-raw $json
    }

    return curl.exe -s -X $Method $url
}

Write-Host "1) Health check" -ForegroundColor Cyan
Invoke-Api -Method GET -Path "/health"

Write-Host "2) Criar pagamento" -ForegroundColor Cyan
$created = Invoke-Api -Method POST -Path "/api/payments" -Body @{
    amount = 199.90
    currency = "BRL"
    method = "Pix"
    description = "Compra A"
    externalReference = "PEDIDO-001"
}
$createdObj = $created | ConvertFrom-Json
$paymentId = $createdObj.id
$created

Write-Host "3) Listar paginado e ordenado" -ForegroundColor Cyan
Invoke-Api -Method GET -Path "/api/payments?page=1&pageSize=10&sortBy=createdAt&sortDir=desc"

Write-Host "4) Buscar por ID" -ForegroundColor Cyan
Invoke-Api -Method GET -Path "/api/payments/$paymentId"

Write-Host "5) Atualizar status para Completed" -ForegroundColor Cyan
Invoke-Api -Method PATCH -Path "/api/payments/$paymentId/status" -Body @{ status = "Completed" }

Write-Host "6) Tentar deletar (esperado 409, pois nao esta Pending)" -ForegroundColor Cyan
curl.exe -i -X DELETE "$BaseUrl/api/payments/$paymentId"
