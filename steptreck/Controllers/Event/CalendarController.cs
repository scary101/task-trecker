using Microsoft.AspNetCore.Mvc;
using steptreck.API.Models;
using steptreck.API.Services.Event;
using steptreck.Domain.DTOs.CalendarDTOs;

namespace steptreck.API.Controllers;

[ApiController]
[Route("api/calendar")]
public class CalendarController : ControllerBase
{
    private readonly CalendarService _calendarService;

    public CalendarController(CalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    [HttpGet("team/{teamId:int}")]
    public async Task<ActionResult<List<CalendarEventDto>>> GetAllEvents(
        int teamId,
        CancellationToken ct)
    {
        var events = await _calendarService.GetAllEventAsync(teamId, ct);
        return Ok(events);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectCalendarEvent>> CreateEvent(
        [FromBody] CreateProjectCalendarEventDto dto,
        CancellationToken ct)
    {

        var createdEvent = await _calendarService.CreateEventAsync(dto, ct);

        return Ok(createdEvent);
    }

    [HttpPut("{eventId:int}")]
    public async Task<ActionResult<ProjectCalendarEvent>> UpdateEvent(
        int eventId,
        [FromBody] UpdateProjectCalendarEventDto dto,
        CancellationToken ct)
    {
        var updatedEvent = await _calendarService.UpdateEventAsync(eventId, dto, ct);
        return Ok(updatedEvent);
    }

    [HttpDelete("{eventId:int}")]
    public async Task<IActionResult> DeleteEvent(
        int eventId,
        CancellationToken ct)
    {
        await _calendarService.DeleteEventAsync(eventId, ct);
        return NoContent();
    }
}