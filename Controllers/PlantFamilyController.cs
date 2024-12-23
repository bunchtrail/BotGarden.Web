 using Microsoft.AspNetCore.Mvc;
using BotGarden.Domain.Models;
using BotGarden.Web.DTOs;
using BotGarden.Infrastructure.Contexts;

namespace BotGarden.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlantFamilyController : ControllerBase
    {
        private readonly BotanicGardenContext _context;

        public PlantFamilyController(BotanicGardenContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<ActionResult<PlantFamilies>> CreateFamily(CreateFamilyDto createFamilyDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var family = new PlantFamilies
            {
                FamilyName = createFamilyDto.FamilyName
            };

            _context.PlantFamilies.Add(family);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFamily), new { id = family.FamilyId }, family);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PlantFamilies>> GetFamily(int id)
        {
            var family = await _context.PlantFamilies.FindAsync(id);

            if (family == null)
                return NotFound();

            return family;
        }
    }
}