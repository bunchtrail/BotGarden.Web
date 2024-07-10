using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Application.DTOs;
using BotGarden.Domain.Models;

namespace BotGarden.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly BotanicGardenContext _context;

        public MapController(BotanicGardenContext context)
        {
            _context = context;
        }

        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var plants = await _context.Plants
                .Where(p => p.Latitude != null && p.Longitude != null)
                .Select(p => new
                {
                    p.PlantId,
                    p.Species,
                    p.Variety,
                    p.Latitude,
                    p.Longitude,
                    p.Note
                })
                .ToListAsync();
            return Ok(plants);
        }

        [HttpGet("GetAllAreas")]
        public async Task<IActionResult> GetAllAreas()
        {
            var areas = await _context.BotGarden
                .Select(a => new
                {
                    a.LocationId,
                    a.LocationPath,
                    Geometry = a.Geometry.ToText()
                })
                .ToListAsync();
            return Ok(areas);
        }

        [HttpPost("AddArea")]
        public async Task<IActionResult> AddArea([FromBody] AddAreaRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var wktReader = new WKTReader();
            var geometry = wktReader.Read(request.Geometry) as Polygon;

            if (geometry == null)
            {
                return BadRequest("Invalid geometry format.");
            }

            var newArea = new BotGardenMode
            {
                LocationPath = request.LocationPath,
                Geometry = geometry
            };

            _context.BotGarden.Add(newArea);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                newArea.LocationId,
                newArea.LocationPath,
            });

        }


        [HttpPut("UpdateArea")]
        public async Task<IActionResult> UpdateArea([FromBody] UpdateAreaRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);

            }

            var area = await _context.BotGarden.FindAsync(request.LocationId);
            if (area == null)
            {
                return NotFound("Area not found.");
            }

            var wktReader = new WKTReader();
            var geometry = wktReader.Read(request.Geometry) as Polygon;

            if (geometry == null)
            {
                return BadRequest("Invalid geometry format.");
            }

            area.Geometry = geometry;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                area.LocationId,
                area.LocationPath,
            });
        }

        [HttpDelete("DeleteArea/{id}")]
        public async Task<IActionResult> DeleteArea(int id)
        {

            var area = await _context.BotGarden.FindAsync(id);
            if (area == null)
            {
                return NotFound("Area not found.");
            }

            _context.BotGarden.Remove(area);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("DeletePlant/{id}")]
        public async Task<IActionResult> DeletePlant(int id)
        {
            var plant = await _context.Plants.FindAsync(id);
            if (plant == null)
            {
                return NotFound();
            }

            _context.Plants.Remove(plant);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("DeletePlantsInArea")]
        public async Task<IActionResult> DeletePlantsInArea([FromBody] PlantIdsDto plantIdsDto)
        {
            if (plantIdsDto == null || plantIdsDto.PlantIds == null || plantIdsDto.PlantIds.Count == 0)
            {
                return BadRequest("Invalid request payload.");
            }

            var plants = await _context.Plants
                .Where(p => plantIdsDto.PlantIds.Contains(p.PlantId))
                .ToListAsync();

            if (plants.Count == 0)
            {
                return NotFound("No plants found in the selected area.");
            }

            _context.Plants.RemoveRange(plants);
            await _context.SaveChangesAsync();

            return Ok("Plants removed successfully.");
        }

    }
}
