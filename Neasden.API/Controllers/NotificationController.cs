using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neasden.API.Dto;
using Neasden.Repository.NotificationCount;
using System.Security.Claims;

namespace Neasden.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly ILogger<NotificationController> _logger;
    private readonly NotificationRetriever _notificationRetriever;
    private readonly INotificationCountRepository _notificationCountRepository;

    public NotificationController(
        ILogger<NotificationController> logger,
        NotificationRetriever notificationRetriever,
        INotificationCountRepository notificationCountRepository)
    {
        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));

        _notificationRetriever = notificationRetriever ?? 
            throw new ArgumentNullException(nameof(notificationRetriever));

        _notificationCountRepository = notificationCountRepository ??
            throw new ArgumentNullException(nameof(notificationCountRepository));
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
    [HttpPost("notificiationRead")]
    public async Task<IActionResult> ReadNotification([FromBody] NotificationReadDto notificationReadDto)
    {
        var deleteNotificationCount = await _notificationCountRepository
            .RemoveFromCountAsync(notificationReadDto.Id);

        if (deleteNotificationCount.IsFailure)
        {
            _logger.LogError("Failed to mark notification as read {NotificationId}: {Error}",
                notificationReadDto.Id, deleteNotificationCount.Error);

            return BadRequest(deleteNotificationCount.Error);
        }

        return Ok();
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

    [Authorize]
    [HttpGet("notificationCount")]
    public async Task<IActionResult> GetNotificationCount()
    {
        var subValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(subValue, out var userId))
        {
            _logger.LogError("{subValue} could not be parsed.", subValue);
            return BadRequest("You need to login to access this endpoint.");
        }

        var count = await _notificationCountRepository.GetUserNotificationCountAsync(userId);

        return Ok(count);
    }

    [Authorize]
    [HttpGet("getByUserIdLatest")]
    public async Task<IActionResult> GetNotificationsByUserIdLatest(
        [FromQuery] DateTime lastChecked,
        CancellationToken cancellationToken = default)
    {
        var subValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(subValue, out var userId))
        {
            _logger.LogError("{subValue} could not be parsed.", subValue);
            return BadRequest("You need to login to access this endpoint.");
        }

        if(lastChecked.Kind == DateTimeKind.Unspecified) {
            lastChecked = DateTime.SpecifyKind(lastChecked, DateTimeKind.Utc);
        }
        else if(lastChecked.Kind == DateTimeKind.Local) {
            lastChecked = lastChecked.ToUniversalTime();
        }

        var notifications = await _notificationRetriever.GetNotificationsByUserLatestIdAsync(
                userId,
                lastChecked,
                cancellationToken);

        if (notifications.IsFailure)
        {
            _logger.LogError("Failed to retrieve latest notifications for user {UserId}: {Error}",
                userId, notifications.Error);

            return BadRequest(notifications.Error);
        }

        return Ok(notifications.Value);
    }
}
