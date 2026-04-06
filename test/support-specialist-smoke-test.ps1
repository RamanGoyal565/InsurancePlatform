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

Write-Host 'Preparing admin, support specialist, customer, and support ticket...'
$adminLogin = Invoke-JsonRequest -Step 'Admin login' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = 'admin@insurance.local'; password = 'Admin@12345' }
$adminHeaders = @{ Authorization = "Bearer $($adminLogin.accessToken)" }

$supportEmail = New-UniqueEmail -Prefix 'supportspecialist'
$supportSpecialist = Invoke-JsonRequest -Step 'Create support specialist' -Method Post -Uri "$GatewayBaseUrl/identity/admin/create-support-specialist" -Headers $adminHeaders -Body @{ name = 'Support Smoke Specialist'; email = $supportEmail; password = 'Support@123' }

$customerEmail = New-UniqueEmail -Prefix 'supportcustomer'
$customer = Invoke-JsonRequest -Step 'Register customer' -Method Post -Uri "$GatewayBaseUrl/identity/auth/register" -Body @{ name = 'Support Customer'; email = $customerEmail; password = 'Customer@123' }
$customerHeaders = @{ Authorization = "Bearer $($customer.accessToken)" }

$ticket = Invoke-JsonRequest -Step 'Create support ticket' -Method Post -Uri "$GatewayBaseUrl/tickets" -Headers $customerHeaders -Body @{ title = 'Support request'; description = 'Need help updating my support ticket.'; type = 1 }

Write-Host 'Logging in as support specialist and handling support ticket...'
$supportLogin = Invoke-JsonRequest -Step 'Support login' -Method Post -Uri "$GatewayBaseUrl/identity/auth/login" -Body @{ email = $supportEmail; password = 'Support@123' }
$supportHeaders = @{ Authorization = "Bearer $($supportLogin.accessToken)" }

$supportTickets = Invoke-JsonRequest -Step 'List support tickets' -Method Get -Uri "$GatewayBaseUrl/tickets" -Headers $supportHeaders
$assigned = Invoke-JsonRequest -Step 'Assign support ticket to support specialist' -Method Put -Uri "$GatewayBaseUrl/tickets/$($ticket.ticketId)/assign" -Headers $adminHeaders -Body @{ assignedToUserId = $supportSpecialist.userId }
$inReview = Invoke-JsonRequest -Step 'Update support ticket status to in review' -Method Put -Uri "$GatewayBaseUrl/tickets/$($ticket.ticketId)/status" -Headers $supportHeaders -Body @{ status = 2 }
$comment = Invoke-JsonRequest -Step 'Add support comment' -Method Post -Uri "$GatewayBaseUrl/tickets/$($ticket.ticketId)/comments" -Headers $supportHeaders -Body @{ message = 'Support specialist is reviewing the ticket.' }
$comments = Invoke-JsonRequest -Step 'Get support comments' -Method Get -Uri "$GatewayBaseUrl/tickets/$($ticket.ticketId)/comments" -Headers $supportHeaders
$resolved = Invoke-JsonRequest -Step 'Resolve support ticket' -Method Put -Uri "$GatewayBaseUrl/tickets/$($ticket.ticketId)/status" -Headers $supportHeaders -Body @{ status = 4 }

$notificationResult = Try-MarkFirstNotificationRead -Headers $supportHeaders

Write-Host "`nSupport specialist smoke test passed." -ForegroundColor Green
[pscustomobject]@{
    SupportSpecialistId = $supportSpecialist.userId
    VisibleSupportTicketCount = $supportTickets.Count
    AssignedTicketTo = $assigned.assignedTo
    InReviewStatus = $inReview.status
    CommentCount = $comments.Count
    FinalTicketStatus = $resolved.status
    NotificationCount = @($notificationResult.Notifications).Count
}
