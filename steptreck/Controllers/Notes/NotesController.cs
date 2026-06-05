using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using steptreck.API.Services.Notes;
using steptreck.Domain.DTOs.NoteDTOs;

[ApiController]
[Route("api/notes")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly NoteService _noteService;

    public NotesController(NoteService notes)
    {
        _noteService = notes;
    }

    [HttpGet]
    public async Task<ActionResult<List<NoteListDto>>> GetList(CancellationToken ct)
    {
        var result = await _noteService.GetNoteListAsync(ct);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NoteDto>> Get(int id, CancellationToken ct)
    {
        var result = await _noteService.GetNoteAsync(id, ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult> Create([FromBody] CreateNoteRequest request, CancellationToken ct)
    {
        await _noteService.CreateNote(request.Title, ct);
        return Ok();
    }

    [HttpPut("{id:int}/content")]
    public async Task<ActionResult> UpdateContent(
        int id,
        [FromBody] UpdateContentRequest request,
        CancellationToken ct)
    {
        await _noteService.SaveContentNote(request.Content, id, ct);
        return Ok();
    }

    [HttpPut("{id:int}/title")]
    public async Task<ActionResult> UpdateTitle(
        int id,
        [FromBody] UpdateTitleRequest request,
        CancellationToken ct)
    {
        await _noteService.SaveTitleNote(request.Title, id, ct);
        return Ok();
    }

    [HttpPatch("{id:int}/pin")]
    public async Task<ActionResult> TogglePin(int id, CancellationToken ct)
    {
        await _noteService.TooglePinnedNoteAsync(id, ct);
        return Ok();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        await _noteService.DeleteNote(id, ct);
        return Ok();
    }
}