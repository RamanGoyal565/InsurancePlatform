param(
    [string]$GatewayBaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"
$contextPath = Join-Path $PSScriptRoot 'satyam-policy-context.json'

function Invoke-JsonRequest {
    param(
        [string]$Step = "",
        [string]$Method,
        [string]$Uri,
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )

    $params = @{ Method = $Method; Uri = $Uri; Headers = $Headers; ContentType = 'application/json' }
    if ($null -ne $Body) { $params.Body = ($Body | ConvertTo-Json -Depth 10) }

    try {
        if ($Step) { Write-Host "[$Step] $Method $Uri" }
        return Invoke-RestMethod @params
    }
    catch {
        $responseText = $null
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $responseText = $reader.ReadToEnd()
            $reader.Dispose()
        }

        Write-Host "`nRequest failed." -ForegroundColor Red
        Write-Host "Step: $Step"
        Write-Host "Method: $Method"
        Write-Host "Uri: $Uri"
        if ($params.Body) { Write-Host "Body: $($params.Body)" }
        if ($responseText) { Write-Host "Response: $responseText" }
        throw
    }
}

if (-not (Test-Path $contextPath)) {
    throw "Missing purchase context file: $contextPath. Run satyam-car-purchase-test.ps1 first."
}

$context = Get-Content -Raw -Path $contextPath | ConvertFrom-Json
$login = Invoke-JsonRequest -Step 'Login Satyam customer' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = $context.email; password = $context.password }
$customerHeaders = @{ Authorization = "Bearer $($login.accessToken)" }

$renew = Invoke-JsonRequest -Step 'Renew Satyam policy' -Method Post -Uri "$GatewayBaseUrl/customer-policies/$($context.customerPolicyId)/renew" -Headers $customerHeaders
Start-Sleep -Seconds 3
$notifications = Invoke-JsonRequest -Step 'Get customer notifications' -Method Get -Uri "$GatewayBaseUrl/notifications" -Headers $customerHeaders

Write-Host "`nSatyam policy renew test passed." -ForegroundColor Green
[pscustomobject]@{
    CustomerId = $login.user.userId
    PolicyId = $context.policyId
    CustomerPolicyId = $context.customerPolicyId
    RenewRequestStatus = $renew.status
    VehicleNumber = $context.vehicleNumber
    DrivingLicenseNumber = $context.drivingLicenseNumber
    NotificationCount = @($notifications).Count
    ContextPath = $contextPath
}
