using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Data;

namespace web.Controllers;

[ApiController]
[Route("api/snacks")]
public class SnacksController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public SnacksController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var list = await _db.Snacks.AsNoTracking().OrderBy(x => x.Category).ThenBy(x => x.Name).ToListAsync();
        return Results.Ok(list);
    }
}