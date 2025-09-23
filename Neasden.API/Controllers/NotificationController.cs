using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neasden.API.Dto;
using Neasden.Repository.Repositories;
using System.Security.Claims;

namespace Neasden.API.Controllers;
[Route("api/[controller]")]
[ApiController]
public class NotificationController : ControllerBase
{
    private readonly NotificationRepository _notificationRepository;
    private readonly DisruptionRepository _disruptionRepository;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        NotificationRepository notificationRepository, 
        DisruptionRepository disruptionRepository,
        ILogger<NotificationController> logger)
    {
        _disruptionRepository = disruptionRepository ?? 
            throw new ArgumentNullException(nameof(disruptionRepository));

        _notificationRepository = notificationRepository ?? 
            throw new ArgumentNullException(nameof(notificationRepository));

        _logger = logger ?? 
            throw new ArgumentNullException(nameof(logger));
    }

    [Authorize]
    [HttpGet("disruption")]
    public async Task<IActionResult> GetDisruptionById(Guid id)
    {
        _logger.LogInformation("Begin retrieving disruption {id}.", id);

        var disruption = await _disruptionRepository
            .GetDisruptionByIdAsync(id);

        if (disruption.IsFailure) {
            return BadRequest(disruption.Error);
        }

        _logger.LogInformation("Successfully retrieved disruption {id}.", id);
        return Ok(disruption.Value);
    }

    [Authorize]
    [HttpGet("description")]
    public async Task<IActionResult> GetDisurptionDescriptionById(Guid id)
    {
        _logger.LogInformation("Begin retrieving disruption description {id}.", id);

        var description = await _disruptionRepository
            .GetDisruptionDescriptionByIdAsync(id);

        if (description.IsFailure) {
            return BadRequest(description.Error);
        }

        _logger.LogInformation("Successfully retrieved disruption description. {id}.", id);
        return Ok(description.Value);
    }

    [Authorize]
    [HttpGet("getById")]
    public async Task<IActionResult> GetNotificationById(Guid id)
    {
        _logger.LogInformation("Begin retrieving notification {id}.", id);

        var notification = await _notificationRepository
            .GetNotificationByIdAsync(id);

        if (notification.IsFailure) {
            return BadRequest(notification.Error);
        }

        var severity = await _disruptionRepository
            .GetDisruptionSeverityByIdAsync(notification.Value.SeverityId);

        if (severity.IsFailure) {
            return BadRequest(severity.Error);
        }

        var notificationVal = notification.Value;

        _logger.LogInformation("Successfully retrieved notification {id}.", id);

        return Ok(new NotificationReturn(
            notificationVal.LineId,
            notificationVal.DisruptionId,
            notificationVal.StartStationId,
            notificationVal.EndStationId,
            severity.Value.Severity,
            notificationVal.NotificationSentBy,
            notificationVal.SentTime));
    }

    [Authorize]
    [HttpGet("getByUserId")]
    public async Task<IActionResult> GetNotificationsByUserId()
    {
        var subValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(subValue, out var userId)) 
        {
            _logger.LogError("{subValue} could not be parsed.", subValue);
            return BadRequest("You need to login to access this endpoint.");
        }

        _logger.LogInformation("Begin retrieving notifications for user {id}.", userId);

        var notifications = await _notificationRepository
            .GetNotificationsByUserId(userId);

        if (notifications.IsFailure) {
            return BadRequest(notifications.Error);
        }

        var results = new List<NotificationReturn>();
        var notificationsVal = notifications.Value;

        foreach (var notification in notificationsVal)
        {
            var severity = await _disruptionRepository
                .GetDisruptionSeverityByIdAsync(notification.SeverityId);

            if (severity.IsFailure) {
                return BadRequest(severity.Error);
            }

            results.Add(new NotificationReturn(
            notification.LineId,
            notification.DisruptionId,
            notification.StartStationId,
            notification.EndStationId,
            severity.Value.Severity,
            notification.NotificationSentBy,
            notification.SentTime));
        }

        _logger.LogInformation("Successfully retrieved notifications for user {id}.", userId);

        return Ok(results);
    }
}
