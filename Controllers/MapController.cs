using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Application.DTOs;
using BotGarden.Domain.Models;
using Microsoft.AspNetCore.Authorization;


namespace BotGarden.Web.Controllers
{
    [ApiController]
    [Authorize]
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
                .Select(p => new PlantDto
                {
                    PlantId = p.PlantId,
                    Species = p.Species,
                    Variety = p.Variety,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Note = p.Note
                })
                .ToListAsync();
            return Ok(plants);
        }

        [HttpGet("GetAllAreas")]
        public async Task<IActionResult> GetAllAreas()
        {
            var areas = await _context.BotGarden
                .Select(a => new AreaDto
                {
                    LocationId = a.LocationId,
                    LocationPath = a.LocationPath,
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

            var geometry = ParseGeometry(request.Geometry);
            if (geometry == null)
            {
                return BadRequest("Invalid geometry format");
            }

            var newArea = new BotGardenMode
            {
                LocationPath = request.LocationPath,
                Geometry = geometry
            };

            _context.BotGarden.Add(newArea);
            await _context.SaveChangesAsync();

            return Ok(new AreaDto
            {
                LocationId = newArea.LocationId,
                LocationPath = newArea.LocationPath,
                Geometry = newArea.Geometry.ToText()
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

            var geometry = ParseGeometry(request.Geometry);
            if (geometry == null)
            {
                return BadRequest("Invalid geometry format.");
            }

            area.Geometry = geometry;
            await _context.SaveChangesAsync();

            return Ok(new AreaDto
            {
                LocationId = area.LocationId,
                LocationPath = area.LocationPath,
                Geometry = area.Geometry.ToText()
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
            if (plantIdsDto?.PlantIds == null || !plantIdsDto.PlantIds.Any())
            {
                return BadRequest("Invalid request payload.");
            }

            var plants = await _context.Plants
                .Where(p => plantIdsDto.PlantIds.Contains(p.PlantId))
                .ToListAsync();

            if (!plants.Any())
            {
                return NotFound("No plants found in the selected area.");
            }

            _context.Plants.RemoveRange(plants);
            await _context.SaveChangesAsync();

            return Ok("Plants removed successfully.");
        }

        private Polygon ParseGeometry(string wkt)
        {
            var wktReader = new WKTReader();
            return wktReader.Read(wkt) as Polygon;
        }
    }

    //TODO: decide what to display
    public class PlantDto
    {
        public int PlantId { get; set; }
        public string Species { get; set; }
        public string Variety { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Note { get; set; }
    }

    public class AreaDto
    {
        public int LocationId { get; set; }
        public string LocationPath { get; set; }
        public string Geometry { get; set; }
    }
}
