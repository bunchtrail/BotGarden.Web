using Microsoft.AspNetCore.Mvc;
using BotGarden.Domain.Models;
using BotGarden.Web.DTOs;
using BotGarden.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace BotGarden.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SectorController : ControllerBase
    {
        private readonly BotanicGardenContext _context;

        public SectorController(BotanicGardenContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Sectors>> CreateSector(CreateSectorDto createSectorDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var sector = new Sectors
            {
                SectorName = createSectorDto.SectorName
            };

            _context.Sectors.Add(sector);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSector), new { id = sector.SectorId }, sector);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Sectors>> GetSector(int id)
        {
            var sector = await _context.Sectors.FindAsync(id);

            if (sector == null)
                return NotFound();

            return sector;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sectors>>> GetAllSectors()
        {
            var sectors = await _context.Sectors.ToListAsync();
            return Ok(sectors);
        }
    }
} 