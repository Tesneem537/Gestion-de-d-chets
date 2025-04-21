using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WasteManagement3.Data;
using WasteManagement3.Models;

[Route("api/[controller]")]
[ApiController]
public class WasteTypeController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WasteTypeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/WasteType
    [HttpGet]
    public async Task<ActionResult<IEnumerable<WasteType>>> GetWasteTypes()
    {
        var wasteTypes = await _context.WasteType.ToListAsync();
        return Ok(wasteTypes);
    }

   
}
