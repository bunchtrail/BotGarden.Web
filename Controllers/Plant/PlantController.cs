using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Application.DTOs;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Domain.Models;
using System.Globalization;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace BotGarden.Web.Controllers.Plant
{
    [Route("api/plant")]
    [ApiController]
    public class PlantController : ControllerBase
    {
        private readonly PlantFamilyService _plantFamilyService;
        private readonly BotGardenService _botGardenService;
        private readonly GenusService _genusService;
        private readonly BotanicGardenContext _context;

        public PlantController(PlantFamilyService plantFamilyService, BotGardenService botGardenService, GenusService genusService, BotanicGardenContext context)
        {
            _plantFamilyService = plantFamilyService;
            _botGardenService = botGardenService;
            _genusService = genusService;
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllPlants([FromQuery] int sectorId)
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
        public async Task<IActionResult> DeletePlant(int id)
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

        [HttpPost("add")]
        public async Task<IActionResult> AddPlant([FromForm] Plants model, [FromForm] string latitude, [FromForm] string longitude)
        {
            latitude = latitude.Replace(',', '.');
            longitude = longitude.Replace(',', '.');

            if (!double.TryParse(latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedLatitude))
            {
                return BadRequest(new { success = false, message = "Invalid latitude value", receivedValue = latitude });
            }

            if (!double.TryParse(longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedLongitude))
            {
                return BadRequest(new { success = false, message = "Invalid longitude value", receivedValue = longitude });
            }

            model.Latitude = parsedLatitude;
            model.Longitude = parsedLongitude;

            ModelState.Remove(nameof(model.Latitude));
            ModelState.Remove(nameof(model.Longitude));

            if (!ModelState.IsValid)
            {
                var detailedErrors = GetModelErrors();
                return BadRequest(new { success = false, message = "Model state is invalid", errors = detailedErrors, model });
            }

            var newPlant = CreatePlantFromModel(model);

            _context.Plants.Add(newPlant);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Plant successfully added!" });
        }

        private object GetModelErrors()
        {
            return ModelState
                .Where(ms => ms.Value.Errors.Count > 0)
                .Select(ms => new
                {
                    Field = ms.Key,
                    Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
                })
                .ToList();
        }

        private Plants CreatePlantFromModel(Plants model)
        {
            return new Plants
            {
                InventorNumber = model.InventorNumber,
                FamilyId = model.FamilyId,
                GenusId = model.GenusId,
                Species = model.Species,
                Synonyms = model.Synonyms,
                Variety = model.Variety,
                Form = model.Form,
                SectorId = model.SectorId,
                PlantOrigin = model.PlantOrigin,
                NaturalHabitat = model.NaturalHabitat,
                Determined = model.Determined,
                EcologyBiology = model.EcologyBiology,
                EconomicUse = model.EconomicUse,
                DateOfPlanting = model.DateOfPlanting,
                Originator = model.Originator,
                Date = model.Date,
                Country = model.Country,
                ProtectionStatus = model.ProtectionStatus,
                HerbariumPresence = model.HerbariumPresence,
                FilledOut = model.FilledOut,
                ImagePath = model.ImagePath,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                HerbariumDuplicate = model.HerbariumDuplicate,
                Note = model.Note,
            };
        }
    }
}
