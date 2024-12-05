using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Application.DTOs;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Domain.Models;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace BotGarden.Web.Controllers.Plant
{
    [Route("api/plant")]
    [ApiController]
    [Authorize]
    public class PlantController : ControllerBase
    {
        private readonly PlantFamilyService _plantFamilyService;
        private readonly BotGardenService _botGardenService;
        private readonly GenusService _genusService;
        private readonly BotanicGardenContext _context;

        public PlantController(
            PlantFamilyService plantFamilyService,
            BotGardenService botGardenService,
            GenusService genusService,
            BotanicGardenContext context)
        {
            _plantFamilyService = plantFamilyService;
            _botGardenService = botGardenService;
            _genusService = genusService;
            _context = context;
        }


        [HttpGet("sector_plant/{sectorId}")]
        public async Task<IActionResult> GetPlantsBySectorId(int sectorId)
        {
            Console.WriteLine(sectorId);
            var plants = await _context.Plants
                .OrderBy(p => p.PlantId)
                .Where(p => p.SectorId == sectorId)
                .ToListAsync();
            Console.WriteLine(plants);
            return Ok(plants);
        }

        [HttpGet("all_families")]
        public async Task<IActionResult> GetAllFamilies()
        {
            var families = await _context.PlantFamilies
                .OrderBy(f => f.FamilyId)
                .ToListAsync();

            return Ok(new { success = true, families });
        }

        [HttpGet("all_genuses")]
        public async Task<IActionResult> GetAllGenuses()
        {
            var genuses = await _context.Genus
                .OrderBy(f => f.GenusId)
                .ToListAsync();

            return Ok(new { genuses });
        }

    

        /// <summary>
        /// Получить все растения в указанном секторе.
        /// </summary>
        /// <param name="sectorId">Идентификатор сектора.</param>
        /// <returns>Список растений.</returns>
        [HttpGet("all")]
        public async Task<IActionResult> GetAllPlants([FromQuery] int sectorId)
        {
            if (sectorId <= 0 || sectorId > 3)
            {
                return BadRequest(new { success = false, message = "Invalid sectorId provided." });
            }

            var plants = await _context.Plants
                .Include(p => p.Family)
                .Include(p => p.Genus)
                .Where(p => p.SectorId == sectorId)
                .OrderBy(p => p.PlantId)
                .ToListAsync();

            return Ok(new { success = true, data = plants });
        }

        /// <summary>
        /// Удалить растение по идентификатору.
        /// </summary>
        /// <param name="id">Идентификатор растения.</param>
        /// <returns>Результат операции.</returns>
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeletePlant(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid plant ID provided." });
            }

            var plant = await _context.Plants.FindAsync(id);
            if (plant == null)
            {
                return NotFound(new { success = false, message = "Plant not found." });
            }

            _context.Plants.Remove(plant);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Plant deleted successfully." });
        }

        /// <summary>
        /// Обновить список растений.
        /// </summary>
        /// <param name="plantUpdates">Список обновлений растений.</param>
        /// <returns>Результат операции.</returns>
        [HttpPost("update")]
        public async Task<IActionResult> UpdatePlants([FromBody] List<PlantUpdateDto> plantUpdates)
        {
            if (plantUpdates == null || !plantUpdates.Any())
            {
                return BadRequest(new { success = false, message = "No data provided." });
            }

            var plantIds = plantUpdates.Select(p => p.PlantId).Distinct().ToList();
            var plantsToUpdate = await _context.Plants
                .Where(p => plantIds.Contains(p.PlantId))
                .ToListAsync();

            if (plantsToUpdate.Count != plantIds.Count)
            {
                return NotFound(new { success = false, message = "One or more plants not found." });
            }

            foreach (var update in plantUpdates)
            {
                var plant = plantsToUpdate.FirstOrDefault(p => p.PlantId == update.PlantId);
                if (plant != null)
                {
                    // Обновление полей, если они предоставлены
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
                    plant.Note = update.Note ?? plant.Note;
                    plant.YearOfObs = update.YearOfObs ?? plant.YearOfObs;
                    plant.PhenophaseDate = update.PhenophaseDate ?? plant.PhenophaseDate;
                    plant.Year = update.Year ?? plant.Year;
                    plant.MeasurementType = update.MeasurementType ?? plant.MeasurementType;
                    plant.Value = update.Value ?? plant.Value;

                    if (update.Latitude.HasValue)
                        plant.Latitude = update.Latitude.Value;

                    if (update.Longitude.HasValue)
                        plant.Longitude = update.Longitude.Value;

                    if (update.HerbariumPresence.HasValue)
                        plant.HerbariumPresence = update.HerbariumPresence.Value;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Plants updated successfully." });
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, new { success = false, message = "A concurrency error occurred while updating the plants." });
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while updating the plants." });
            }
        }

        /// <summary>
        /// Добавить новое растение.
        /// </summary>
        /// <param name="model">Модель растения.</param>
        /// <returns>Результат операции.</returns>
        [HttpPost("add")]
        public async Task<IActionResult> AddPlant([FromBody] PlantCreateDto model)
        {
            if (model == null)
            {
                return BadRequest(new { success = false, message = "No plant data provided." });
            }

            var errorDetails = new List<string>();

            // Validate SectorId
            if (model.SectorId <= 0)
            {
                errorDetails.Add("Invalid SectorId provided.");
            }

            // Parse coordinates
            if (!double.TryParse(model.Latitude?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLatitude))
            {
                errorDetails.Add($"Invalid latitude value: '{model.Latitude}'.");
            }

            if (!double.TryParse(model.Longitude?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLongitude))
            {
                errorDetails.Add($"Invalid longitude value: '{model.Longitude}'.");
            }

            // Handle BiometricId based on SectorId
            if (model.SectorId == 2)
            {
                if (model.BiometricId == null)
                {
                    errorDetails.Add("BiometricId is required for SectorId = 2.");
                }

            }
            else
            {
                // Ensure BiometricId is not set when SectorId is not 2
                model.BiometricId = null;
            }

            // If there are validation errors, return them
            if (errorDetails.Any())
            {
                return BadRequest(new { success = false, message = "Validation errors occurred.", errors = errorDetails });
            }

            // Initialize new plant
            var newPlant = new Plants
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
                Latitude = parsedLatitude,
                Longitude = parsedLongitude,
                HerbariumDuplicate = model.HerbariumDuplicate,
                Note = model.Note,
                YearOfObs = model.YearOfObs,
                PhenophaseDate = model.PhenophaseDate,
                Year = model.Year,
                MeasurementType = model.MeasurementType,
                Value = model.Value,
                BiometricId = model.BiometricId
            };

            // Model state validation
            if (!TryValidateModel(newPlant))
            {
                var detailedErrors = GetModelErrors();
                return BadRequest(new { success = false, message = "Model state is invalid.", errors = detailedErrors });
            }

            _context.Plants.Add(newPlant);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Plant successfully added!", data = newPlant });
            }
            catch (DbUpdateException dbEx)
            {
                // Log the exception details (optional)
                // _logger.LogError(dbEx, "An error occurred while adding the plant.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while adding the plant.",
                    error = dbEx.Message // Include exception message for debugging (remove in production)
                });
            }
            catch (Exception ex)
            {
                // Log the exception details (optional)
                // _logger.LogError(ex, "An unexpected error occurred.");

                return StatusCode(500, new
                {
                    success = false,
                    message = "An unexpected error occurred while adding the plant.",
                    error = ex.Message // Include exception message for debugging (remove in production)
                });
            }
        }
        /// <summary>
        /// Получить ошибки модели.
        /// </summary>
        /// <returns>Список ошибок.</returns>
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
    }
}
