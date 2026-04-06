param(
    [string]$GatewayBaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"

function New-UniqueEmail {
    param([string]$Prefix)
    return "{0}_{1}@test.local" -f $Prefix, ([guid]::NewGuid().ToString("N").Substring(0, 8))
}

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
        Write-Host "`nSmoke test request failed." -ForegroundColor Red
        Write-Host "Step: $Step"
        Write-Host "Method: $Method"
        Write-Host "Uri: $Uri"
        if ($params.Body) { Write-Host "Body: $($params.Body)" }
        if ($responseText) { Write-Host "Response: $responseText" }
        throw
    }
}

function Try-MarkFirstNotificationRead {
    param([hashtable]$Headers)

    $notifications = Invoke-JsonRequest -Step 'Get notifications' -Method Get -Uri "$GatewayBaseUrl/notifications" -Headers $Headers
    if ($notifications -is [array] -and $notifications.Count -gt 0) {
        $marked = Invoke-JsonRequest -Step 'Mark first notification read' -Method Post -Uri "$GatewayBaseUrl/notifications/$($notifications[0].notificationId)/read" -Headers $Headers -Body @{ isRead = $true }
        return [pscustomobject]@{ Notifications = $notifications; Marked = $marked }
    }

    return [pscustomobject]@{ Notifications = @(); Marked = $null }
}

Write-Host 'Logging in as seeded admin through Ocelot...'
$adminLogin = Invoke-JsonRequest -Step 'Admin login' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = 'admin@insurance.local'; password = 'Admin@12345' }
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.accessToken)" }

$claimsSpecialist = Invoke-JsonRequest -Step 'Create claims specialist' -Method Post -Uri "$GatewayBaseUrl/identity/admin/create-claims-specialist" -Headers $adminHeaders -Body @{ name = 'Claims Specialist Smoke'; email = (New-UniqueEmail -Prefix 'claims'); password = 'Claims@123' }
$supportSpecialist = Invoke-JsonRequest -Step 'Create support specialist' -Method Post -Uri "$GatewayBaseUrl/identity/admin/create-support-specialist" -Headers $adminHeaders -Body @{ name = 'Support Specialist Smoke'; email = (New-UniqueEmail -Prefix 'support'); password = 'Support@123' }
$users = Invoke-JsonRequest -Step 'List users' -Method Get -Uri "$GatewayBaseUrl/identity/admin/users" -Headers $adminHeaders
$deactivated = Invoke-JsonRequest -Step 'Deactivate support specialist' -Method Patch -Uri "$GatewayBaseUrl/identity/admin/users/$($supportSpecialist.userId)/status" -Headers $adminHeaders -Body @{ isActive = $false }
$reactivated = Invoke-JsonRequest -Step 'Reactivate support specialist' -Method Patch -Uri "$GatewayBaseUrl/identity/admin/users/$($supportSpecialist.userId)/status" -Headers $adminHeaders -Body @{ isActive = $true }

$createdPolicy = Invoke-JsonRequest -Step 'Create policy' -Method Post -Uri "$GatewayBaseUrl/policies" -Headers $adminHeaders -Body @{
    name = 'Admin Smoke Car Policy'; vehicleType = 'Car'; premium = 1999.50; coverageDetails = 'Collision, theft, and roadside assistance coverage.'; terms = 'Valid for one year from start date.'; policyDocument = 'Admin smoke test generated vehicle policy document.'
}
$policies = Invoke-JsonRequest -Step 'List policies' -Method Get -Uri "$GatewayBaseUrl/policies"
$updatedPolicy = Invoke-JsonRequest -Step 'Update policy' -Method Put -Uri "$GatewayBaseUrl/policies/$($createdPolicy.policyId)" -Headers $adminHeaders -Body @{
    name = 'Admin Smoke Truck Policy'; vehicleType = 'Truck'; premium = 2499.75; coverageDetails = 'Commercial truck collision, theft, and towing coverage.'; terms = 'Updated annual vehicle policy terms.'; policyDocument = 'Updated vehicle policy document from admin smoke test.'
}
$policyDocument = Invoke-JsonRequest -Step 'Get policy document' -Method Get -Uri "$GatewayBaseUrl/policies/$($createdPolicy.policyId)/document"

$adminTickets = Invoke-JsonRequest -Step 'List tickets as admin' -Method Get -Uri "$GatewayBaseUrl/tickets" -Headers $adminHeaders
$notificationResult = Try-MarkFirstNotificationRead -Headers $adminHeaders
$dashboard = Invoke-JsonRequest -Step 'Get dashboard' -Method Get -Uri "$GatewayBaseUrl/admin/dashboard" -Headers $adminHeaders
$reportEvents = Invoke-JsonRequest -Step 'Get reports events' -Method Get -Uri "$GatewayBaseUrl/admin/reports/events" -Headers $adminHeaders

Write-Host "`nAdmin smoke test passed." -ForegroundColor Green
[pscustomobject]@{
    ClaimsSpecialistId = $claimsSpecialist.userId
    SupportSpecialistId = $supportSpecialist.userId
    UserCount = $users.Count
    SupportSpecialistDeactivated = $deactivated.isActive
    SupportSpecialistReactivated = $reactivated.isActive
    PolicyCount = $policies.Count
    UpdatedPolicyName = $updatedPolicy.name
    PolicyDocumentName = $policyDocument.name
    VisibleTicketCount = $adminTickets.Count
    NotificationCount = @($notificationResult.Notifications).Count
    DashboardTotalEvents = $dashboard.totalEvents
    ReportEventCount = $reportEvents.Count
}

