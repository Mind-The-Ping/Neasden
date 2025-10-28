using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Neasden.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly ILogger<NotificationController> _logger;
    private readonly NotificationRetriever _notificationRetriever;

    public NotificationController(
        ILogger<NotificationController> logger,
        NotificationRetriever notificationRetriever)
    {
        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));

        _notificationRetriever = notificationRetriever ?? 
            throw new ArgumentNullException(nameof(notificationRetriever));
    }

    [Authorize]
    [HttpGet("getById")]
    public async Task<IActionResult> GetNotificationById(
        Guid id, 
        CancellationToken cancellationToken = default)
    {
        var notification  = await _notificationRetriever.GetNotificiationAsnyc(id, cancellationToken);

        if(notification.IsFailure) 
        {
            _logger.LogError("Failed to retrieve notification {NotificationId}: {Error}", 
                id, notification.Error);

            return BadRequest(notification.Error);
        }

        return Ok(notification.Value);
    }

    [Authorize]
    [HttpGet("getByUserId")]
    public async Task<IActionResult> GetNotificationsByUserId(
        [FromQuery]int page, 
        [FromQuery]int pageSize,
        CancellationToken cancellationToken = default)
    {
        var subValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(subValue, out var userId)) 
        {
            _logger.LogError("{subValue} could not be parsed.", subValue);
            return BadRequest("You need to login to access this endpoint.");
        }

        var notifications = await _notificationRetriever.GetNotificationsByUserIdAsync(
            userId, 
            page, 
            pageSize, 
            cancellationToken);

        if(notifications.IsFailure)
        {
            _logger.LogError("Failed to retrieve notifications for user {UserId}: {Error}",
                userId, notifications.Error);

            return BadRequest(notifications.Error);
        }

        return Ok(notifications.Value);
    }
}
