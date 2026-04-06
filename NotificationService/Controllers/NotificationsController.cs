using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.DTOs;
using NotificationService.Services;

namespace NotificationService.Controllers;

[ApiController]
[Authorize]
[Route("notifications")]
public sealed class NotificationsController(INotificationService notificationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken) => Ok(await notificationService.GetAsync(User.ToCurrentUser(), cancellationToken));

    [HttpPost("{notificationId:guid}/read")]
    public async Task<ActionResult> MarkRead(Guid notificationId, MarkNotificationReadRequest request, CancellationToken cancellationToken) => Ok(await notificationService.MarkReadAsync(notificationId, User.ToCurrentUser(), request.IsRead, cancellationToken));
}
