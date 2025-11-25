using DevOpsProject.Bms.Logic.Data;
using DevOpsProject.Shared.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevOpsProject.Bms.API.Controllers;

[ApiController]
[Route("api/v1/reposition")]
public class RepositionController : ControllerBase
{
    private readonly BmsDbContext _db;

    public RepositionController(BmsDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IEnumerable<HiveRepositionSuggestion>> GetActiveSuggestions(CancellationToken ct)
    {
        return await _db.HiveRepositionSuggestions
            .Where(s => !s.IsConsumed)
            .OrderByDescending(s => s.SuggestedAtUtc)
            .ToListAsync(ct);
    }
}