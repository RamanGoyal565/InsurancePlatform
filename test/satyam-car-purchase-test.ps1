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

function Get-OrCreateSatyamCustomer {
    param([string]$BaseUrl)

    $email = 'satyam@test.com'
    $password = 'Satyam@123'
    $name = 'Satyam'

    try {
        $registration = Invoke-JsonRequest -Step 'Register Satyam customer' -Method Post -Uri "$BaseUrl/identity/auth/register" -Body @{ name = $name; email = $email; password = $password }
        return [pscustomobject]@{
            UserId = $registration.user.userId
            AccessToken = $registration.accessToken
            Registered = $true
        }
    }
    catch {
        Write-Host 'Registration skipped, trying login for existing Satyam account...'
        $login = Invoke-JsonRequest -Step 'Login Satyam customer' -Method Post -Uri "$BaseUrl/identity/auth/login" -Body @{ email = $email; password = $password }
        return [pscustomobject]@{
            UserId = $login.user.userId
            AccessToken = $login.accessToken
            Registered = $false
        }
    }
}

$satyam = Get-OrCreateSatyamCustomer -BaseUrl $GatewayBaseUrl
$customerHeaders = @{ Authorization = "Bearer $($satyam.AccessToken)" }

$adminLogin = Invoke-JsonRequest -Step 'Admin login' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = 'admin@insurance.local'; password = 'Admin@12345' }
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.accessToken)" }

$policy = Invoke-JsonRequest -Step 'Create car policy for purchase test' -Method Post -Uri "$GatewayBaseUrl/policies" -Headers $adminHeaders -Body @{
    name = 'Satyam Car Policy'
    vehicleType = 1
    premium = 1800.00
    coverageDetails = 'Car accident, theft, third-party, and roadside assistance coverage.'
    terms = 'Valid for one year from activation date.'
    policyDocument = 'Satyam car insurance policy document.'
}

$policyDocument = Invoke-JsonRequest -Step 'Get policy document' -Method Get -Uri "$GatewayBaseUrl/policies/$($policy.policyId)/document"
$purchase = Invoke-JsonRequest -Step 'Purchase car policy for Satyam' -Method Post -Uri "$GatewayBaseUrl/purchase" -Headers $customerHeaders -Body @{
    policyId = $policy.policyId
    vehicleNumber = 'UP32ST1234'
    drivingLicenseNumber = 'DL-SATYAM-2026-12345'
    paymentReference = 'PAY-SATYAM-CAR-001'
}

Start-Sleep -Seconds 3
$notifications = Invoke-JsonRequest -Step 'Get customer notifications' -Method Get -Uri "$GatewayBaseUrl/notifications" -Headers $customerHeaders

[pscustomobject]@{
    customerId = $satyam.UserId
    email = 'satyam@test.com'
    password = 'Satyam@123'
    policyId = $policy.policyId
    customerPolicyId = $purchase.customerPolicyId
    vehicleNumber = $purchase.vehicleNumber
    drivingLicenseNumber = $purchase.drivingLicenseNumber
} | ConvertTo-Json -Depth 5 | Set-Content -Path $contextPath

Write-Host "`nSatyam car purchase test passed." -ForegroundColor Green
[pscustomobject]@{
    RegisteredNewCustomer = $satyam.Registered
    CustomerId = $satyam.UserId
    PolicyId = $policy.policyId
    PolicyDocumentName = $policyDocument.name
    CustomerPolicyId = $purchase.customerPolicyId
    PurchaseStatus = $purchase.status
    VehicleNumber = $purchase.vehicleNumber
    DrivingLicenseNumber = $purchase.drivingLicenseNumber
    NotificationCount = @($notifications).Count
    SavedContextPath = $contextPath
}
