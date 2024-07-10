using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BotGarden.Application.DTOs;
using BotGarden.Infrastructure.Contexts;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotGarden.Web.Controllers
{
    [Route("api/dendrology")]
    [ApiController]
    public class DendrologyAllController : ControllerBase
    {
        private readonly BotanicGardenContext _context;

        public DendrologyAllController(BotanicGardenContext context)
        {
            _context = context;
        }

        [HttpGet("plants")]
        public async Task<IActionResult> GetPlants([FromQuery] int sectorId)
        {
            var plants = await _context.Plants
                .Include(p => p.Family)
                .Include(p => p.Genus)
                .Where(p => p.SectorId == sectorId)
                .OrderBy(p => p.PlantId)
                .ToListAsync();

            return Ok(plants);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var plant = await _context.Plants.FindAsync(id);
            if (plant == null)
            {
                return NotFound(new { success = false, message = "Plant not found." });
            }

            _context.Plants.Remove(plant);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Plant deleted successfully." });
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdatePlants([FromBody] List<PlantUpdateDto> plantUpdates)
        {
            if (plantUpdates == null || plantUpdates.Count == 0)
                return BadRequest("No data provided");

            var plantIds = plantUpdates.Select(p => p.PlantId).ToList();
            var plantsToUpdate = await _context.Plants.Where(p => plantIds.Contains(p.PlantId)).ToListAsync();

            foreach (var update in plantUpdates)
            {
                var plant = plantsToUpdate.FirstOrDefault(p => p.PlantId == update.PlantId);
                if (plant != null)
                {
                    plant.FamilyId = update.FamilyId ?? plant.FamilyId;
                    plant.BiometricId = update.BiometricId ?? plant.BiometricId;
                    plant.SectorId = update.SectorId ?? plant.SectorId;
                    plant.GenusId = update.GenusId ?? plant.GenusId;
                    plant.InventorNumber = update.InventorNumber ?? plant.InventorNumber;
                    plant.Species = update.Species ?? plant.Species;
                    plant.Variety = update.Variety ?? plant.Variety;
                    plant.Form = update.Form ?? plant.Form;
                    plant.Determined = update.Determined ?? plant.Determined;
                    plant.DateOfPlanting = update.DateOfPlanting ?? plant.DateOfPlanting;
                    plant.ProtectionStatus = update.ProtectionStatus ?? plant.ProtectionStatus;
                    plant.FilledOut = update.FilledOut ?? plant.FilledOut;
                    plant.HerbariumDuplicate = update.HerbariumDuplicate ?? plant.HerbariumDuplicate;
                    plant.Synonyms = update.Synonyms ?? plant.Synonyms;
                    plant.PlantOrigin = update.PlantOrigin ?? plant.PlantOrigin;
                    plant.NaturalHabitat = update.NaturalHabitat ?? plant.NaturalHabitat;
                    plant.EcologyBiology = update.EcologyBiology ?? plant.EcologyBiology;
                    plant.EconomicUse = update.EconomicUse ?? plant.EconomicUse;
                    plant.Originator = update.Originator ?? plant.Originator;
                    plant.Date = update.Date ?? plant.Date;
                    plant.Country = update.Country ?? plant.Country;
                    plant.ImagePath = update.ImagePath ?? plant.ImagePath;
                    plant.Latitude = update.Latitude ?? plant.Latitude;
                    plant.Longitude = update.Longitude ?? plant.Longitude;

                    if (update.HerbariumPresence.HasValue)
                        plant.HerbariumPresence = update.HerbariumPresence.Value;

                    plant.Note = update.Note ?? plant.Note;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Plants updated successfully." });
        }
    }
}
