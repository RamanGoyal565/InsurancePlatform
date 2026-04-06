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

Write-Host 'Preparing admin, claims specialist, customer, and claim tickets...'
$adminLogin = Invoke-JsonRequest -Step 'Admin login' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = 'admin@insurance.local'; password = 'Admin@12345' }
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.accessToken)" }

$claimsEmail = New-UniqueEmail -Prefix 'claimsspecialist'
$claimsSpecialist = Invoke-JsonRequest -Step 'Create claims specialist' -Method Post -Uri "$GatewayBaseUrl/identity/admin/create-claims-specialist" -Headers $adminHeaders -Body @{ name = 'Claims Smoke Specialist'; email = $claimsEmail; password = 'Claims@123' }

$customerEmail = New-UniqueEmail -Prefix 'claimcustomer'
$customer = Invoke-JsonRequest -Step 'Register customer' -Method Post -Uri "$GatewayBaseUrl/identity/auth/register" -Body @{ name = 'Claims Customer'; email = $customerEmail; password = 'Customer@123' }
$customerHeaders = @{ Authorization = "Bearer $($customer.accessToken)" }

$policy = Invoke-JsonRequest -Step 'Create setup policy' -Method Post -Uri "$GatewayBaseUrl/policies" -Headers $adminHeaders -Body @{ name = 'Claims Smoke Car Policy'; vehicleType = 'Car'; premium = 1599.00; coverageDetails = 'Collision and theft coverage.'; terms = 'One-year vehicle policy.'; policyDocument = 'Claims specialist smoke document.' }

$claimTicketApprove = Invoke-JsonRequest -Step 'Create claim ticket to approve' -Method Post -Uri "$GatewayBaseUrl/tickets" -Headers $customerHeaders -Body @{ title = 'Vehicle claim approve'; description = 'Accident claim submitted for approval.'; type = 2; policyId = $policy.policyId; claimAmount = 4200.00; documents = 'approve.pdf' }
$claimTicketReject = Invoke-JsonRequest -Step 'Create claim ticket to reject' -Method Post -Uri "$GatewayBaseUrl/tickets" -Headers $customerHeaders -Body @{ title = 'Vehicle claim reject'; description = 'Claim submitted for rejection path.'; type = 2; policyId = $policy.policyId; claimAmount = 3100.00; documents = 'reject.pdf' }

Write-Host 'Logging in as claims specialist and processing claims...'
$claimsLogin = Invoke-JsonRequest -Step 'Claims specialist login' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = $claimsEmail; password = 'Claims@123' }
$claimsHeaders = @{ Authorization = "Bearer $($claimsLogin.accessToken)" }

$claimTickets = Invoke-JsonRequest -Step 'List claim tickets' -Method Get -Uri "$GatewayBaseUrl/tickets" -Headers $claimsHeaders
$assignedForApproval = Invoke-JsonRequest -Step 'Assign approve claim ticket to claims specialist' -Method Put -Uri "$GatewayBaseUrl/tickets/$($claimTicketApprove.ticketId)/assign" -Headers $adminHeaders -Body @{ assignedToUserId = $claimsSpecialist.userId }
$assignedForReject = Invoke-JsonRequest -Step 'Assign reject claim ticket to claims specialist' -Method Put -Uri "$GatewayBaseUrl/tickets/$($claimTicketReject.ticketId)/assign" -Headers $adminHeaders -Body @{ assignedToUserId = $claimsSpecialist.userId }
$inReview = Invoke-JsonRequest -Step 'Update first claim ticket status' -Method Put -Uri "$GatewayBaseUrl/tickets/$($claimTicketApprove.ticketId)/status" -Headers $claimsHeaders -Body @{ status = 2 }
$comment = Invoke-JsonRequest -Step 'Add claims comment' -Method Post -Uri "$GatewayBaseUrl/tickets/$($claimTicketApprove.ticketId)/comments" -Headers $claimsHeaders -Body @{ message = 'Claim documents verified.' }
$comments = Invoke-JsonRequest -Step 'Get claim comments' -Method Get -Uri "$GatewayBaseUrl/tickets/$($claimTicketApprove.ticketId)/comments" -Headers $claimsHeaders
$approved = Invoke-JsonRequest -Step 'Approve claim' -Method Post -Uri "$GatewayBaseUrl/tickets/$($claimTicketApprove.ticketId)/approve" -Headers $claimsHeaders
$rejected = Invoke-JsonRequest -Step 'Reject claim' -Method Post -Uri "$GatewayBaseUrl/tickets/$($claimTicketReject.ticketId)/reject" -Headers $claimsHeaders

$notificationResult = Try-MarkFirstNotificationRead -Headers $claimsHeaders

Write-Host "`nClaims specialist smoke test passed." -ForegroundColor Green
[pscustomobject]@{
    ClaimsSpecialistId = $claimsSpecialist.userId
    VisibleClaimTicketCount = $claimTickets.Count
    AssignedApproveTicketTo = $assignedForApproval.assignedTo
    AssignedRejectTicketTo = $assignedForReject.assignedTo
    InReviewStatus = $inReview.status
    CommentCount = $comments.Count
    ApprovedTicketStatus = $approved.status
    ApprovedClaimDecision = $approved.claimDetails.approvalStatus
    RejectedTicketStatus = $rejected.status
    RejectedClaimDecision = $rejected.claimDetails.approvalStatus
    NotificationCount = @($notificationResult.Notifications).Count
}
