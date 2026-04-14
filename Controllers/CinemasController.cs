using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Data;

namespace web.Controllers;

[ApiController]
[Route("api/cinemas")]
public class CinemasController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CinemasController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IResult> Get()
    {
        var list = await _db.Cinemas.AsNoTracking().OrderBy(x => x.Name).ToListAsync();
        return Results.Ok(list);
    }
}
