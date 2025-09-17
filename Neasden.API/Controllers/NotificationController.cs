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

    public NotificationController(
        NotificationRepository notificationRepository, 
        DisruptionRepository disruptionRepository)
    {
        _disruptionRepository = disruptionRepository;
        _notificationRepository = notificationRepository;
    }

    [Authorize]
    [HttpGet("disruption")]
    public async Task<IActionResult> GetDisruptionById(Guid id)
    {
        var disruption = await _disruptionRepository
            .GetDisruptionByIdAsync(id);

        if (disruption.IsFailure) {
            return BadRequest(disruption.Error);
        }
       
        return Ok(disruption.Value);
    }

    [Authorize]
    [HttpGet("description")]
    public async Task<IActionResult> GetDisurptionDescriptionById(Guid id)
    {
        var description = await _disruptionRepository
            .GetDisruptionDescriptionByIdAsync(id);

        if (description.IsFailure) {
            return BadRequest(description.Error);
        }

        return Ok(description.Value);
    }

    [Authorize]
    [HttpGet("getById")]
    public async Task<IActionResult> GetNotificationById(Guid id)
    {
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

        if (!Guid.TryParse(subValue, out var userId)) {
            return BadRequest("You need to login to access this endpoint.");
        }

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

        return Ok(results);
    }
}
