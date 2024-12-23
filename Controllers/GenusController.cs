 using Microsoft.AspNetCore.Mvc;
using BotGarden.Domain.Models;
using BotGarden.Web.DTOs;
using BotGarden.Infrastructure.Contexts;

namespace BotGarden.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenusController : ControllerBase
    {
        private readonly BotanicGardenContext _context;

        public GenusController(BotanicGardenContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<Genus>> CreateGenus(CreateGenusDto createGenusDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var genus = new Genus
            {
                GenusName = createGenusDto.GenusName
            };

            _context.Genus.Add(genus);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGenus), new { id = genus.GenusId }, genus);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Genus>> GetGenus(int id)
        {
            var genus = await _context.Genus.FindAsync(id);

            if (genus == null)
                return NotFound();

            return genus;
        }
    }
}