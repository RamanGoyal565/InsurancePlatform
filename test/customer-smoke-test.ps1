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

$email = New-UniqueEmail -Prefix 'customer'

$registration = Invoke-JsonRequest -Step 'Register customer' -Method Post -Uri "$GatewayBaseUrl/identity/auth/register" -Body @{ name = 'Customer Smoke'; email = $email; password = 'Customer@123' }
$customerId = $registration.user.userId
$customerHeaders = @{ Authorization = "Bearer $($registration.accessToken)" }

$login = Invoke-JsonRequest -Step 'Customer login' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = $email; password = 'Customer@123' }
$customerHeaders = @{ Authorization = "Bearer $($login.accessToken)" }

$adminLogin = Invoke-JsonRequest -Step 'Admin setup login' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = 'admin@insurance.local'; password = 'Admin@12345' }
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.accessToken)" }
$policy = Invoke-JsonRequest -Step 'Create setup policy' -Method Post -Uri "$GatewayBaseUrl/policies" -Headers $adminHeaders -Body @{ name = 'Customer Smoke Bike Policy'; vehicleType = 'Bike'; premium = 899.99; coverageDetails = 'Bike accident, theft, and roadside coverage.'; terms = 'Vehicle plan valid for one year.'; policyDocument = 'Customer smoke test vehicle policy document.' }

$policies = Invoke-JsonRequest -Step 'List policies' -Method Get -Uri "$GatewayBaseUrl/policies"
$policyDocument = Invoke-JsonRequest -Step 'Get policy document' -Method Get -Uri "$GatewayBaseUrl/policies/$($policy.policyId)/document"
$payment = Invoke-JsonRequest -Step 'Create payment as customer' -Method Post -Uri "$GatewayBaseUrl/payments" -Headers $customerHeaders -Body @{ customerId = $customerId; policyId = $policy.policyId; amount = 899.99 }
$customerPolicy = Invoke-JsonRequest -Step 'Purchase policy' -Method Post -Uri "$GatewayBaseUrl/purchase" -Headers $customerHeaders -Body @{ policyId = $policy.policyId; vehicleNumber = 'DL01AB1234'; drivingLicenseNumber = 'DL-042026-998877'; paymentReference = 'PAY-CUSTOMER-SMOKE' }
Start-Sleep -Seconds 3
$renewedCustomerPolicy = Invoke-JsonRequest -Step 'Renew customer policy' -Method Post -Uri "$GatewayBaseUrl/customer-policies/$($customerPolicy.customerPolicyId)/renew" -Headers $customerHeaders

$supportTicket = Invoke-JsonRequest -Step 'Create support ticket' -Method Post -Uri "$GatewayBaseUrl/tickets" -Headers $customerHeaders -Body @{ title = 'Support request'; description = 'Need support from customer smoke test.'; type = 1 }
$claimTicket = Invoke-JsonRequest -Step 'Create claim ticket' -Method Post -Uri "$GatewayBaseUrl/tickets" -Headers $customerHeaders -Body @{ title = 'Claim assistance needed'; description = 'Customer smoke claim ticket.'; type = 2; policyId = $policy.policyId; claimAmount = 2500.00; documents = 'claim.pdf' }
$tickets = Invoke-JsonRequest -Step 'List customer tickets' -Method Get -Uri "$GatewayBaseUrl/tickets" -Headers $customerHeaders
$addedComment = Invoke-JsonRequest -Step 'Add customer comment' -Method Post -Uri "$GatewayBaseUrl/tickets/$($claimTicket.ticketId)/comments" -Headers $customerHeaders -Body @{ message = 'First customer comment.' }
$comments = Invoke-JsonRequest -Step 'Get customer comments' -Method Get -Uri "$GatewayBaseUrl/tickets/$($claimTicket.ticketId)/comments" -Headers $customerHeaders

$notificationResult = Try-MarkFirstNotificationRead -Headers $customerHeaders

Write-Host "`nCustomer smoke test passed." -ForegroundColor Green
[pscustomobject]@{
    CustomerId = $customerId
    PolicyCount = $policies.Count
    PolicyDocumentName = $policyDocument.name
    PaymentId = $payment.paymentId
    CustomerPolicyId = $customerPolicy.customerPolicyId
    PurchaseStatus = $customerPolicy.status
    VehicleNumber = $customerPolicy.vehicleNumber
    DrivingLicenseNumber = $customerPolicy.drivingLicenseNumber
    RenewRequestStatus = $renewedCustomerPolicy.status
    SupportTicketId = $supportTicket.ticketId
    ClaimTicketId = $claimTicket.ticketId
    VisibleTicketCount = $tickets.Count
    CommentCount = $comments.Count
    NotificationCount = @($notificationResult.Notifications).Count
}
