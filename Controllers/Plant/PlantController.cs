using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using BotGarden.Application.DTOs;
using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Domain.Models;
using BotGarden.Infrastructure.Contexts;

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
        private readonly ILogger<PlantController> _logger;

        public PlantController(
            PlantFamilyService plantFamilyService,
            BotGardenService botGardenService,
            GenusService genusService,
            BotanicGardenContext context,
            ILogger<PlantController> logger)
        {
            _plantFamilyService = plantFamilyService;
            _botGardenService = botGardenService;
            _genusService = genusService;
            _context = context;
            _logger = logger;
        }

        #region Helper Methods

        private IActionResult CreateResponse(bool success, string message, object? data = null, object? errors = null)
        {
            var response = new Dictionary<string, object>
            {
                { "success", success },
                { "message", message }
            };

            if (data != null)
                response.Add("data", data);

            if (errors != null)
                response.Add("errors", errors);

            return Ok(response);
        }

        private IActionResult CreateErrorResponse(string message, object? errors = null, int statusCode = 400)
        {
            var response = new Dictionary<string, object>
            {
                { "success", false },
                { "message", message }
            };

            if (errors != null)
                response.Add("errors", errors);

            return StatusCode(statusCode, response);
        }

        private async Task<List<Plants>> GetPlantsBySectorAsync(int sectorId, bool includeRelated = false)
        {
            IQueryable<Plants> query = _context.Plants.Where(p => p.SectorId == sectorId);

            if (includeRelated)
            {
                query = query.Include(p => p.Family)
                             .Include(p => p.Genus);
            }

            return await query.OrderBy(p => p.PlantId).ToListAsync();
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

        #endregion

        #region GET Endpoints

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
                return CreateErrorResponse("Invalid sectorId provided.");
            }

            var plants = await GetPlantsBySectorAsync(sectorId, includeRelated: true);
            return CreateResponse(true, "Plants retrieved successfully.", plants);
        }

        /// <summary>
        /// Получить растения по идентификатору сектора.
        /// </summary>
        /// <param name="sectorId">Идентификатор сектора.</param>
        /// <returns>Список растений.</returns>
        [HttpGet("sector_plant/{sectorId}")]
        public async Task<IActionResult> GetPlantsBySectorId(int sectorId)
        {
            _logger.LogInformation("Fetching plants for SectorId: {SectorId}", sectorId);
            var plants = await GetPlantsBySectorAsync(sectorId);
            _logger.LogInformation("Retrieved {Count} plants for SectorId: {SectorId}", plants.Count, sectorId);
            return CreateResponse(true, "Plants retrieved successfully.", plants);
        }

        /// <summary>
        /// Получить все семейства растений.
        /// </summary>
        /// <returns>Список семейств.</returns>
        [HttpGet("all_families")]
        public async Task<IActionResult> GetAllFamilies()
        {
            var families = await _context.PlantFamilies
                .OrderBy(f => f.FamilyId)
                .ToListAsync();

            return CreateResponse(true, "Families retrieved successfully.", families);
        }

        /// <summary>
        /// Получить все роды растений.
        /// </summary>
        /// <returns>Список родов.</returns>
        [HttpGet("all_genuses")]
        public async Task<IActionResult> GetAllGenuses()
        {
            var genuses = await _context.Genus
                .OrderBy(g => g.GenusId)
                .ToListAsync();

            return CreateResponse(true, "Genuses retrieved successfully.", genuses);
        }

        #endregion

        #region DELETE Endpoint

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
                return CreateErrorResponse("Invalid plant ID provided.");
            }

            var plant = await _context.Plants.FindAsync(id);
            if (plant == null)
            {
                return CreateErrorResponse("Plant not found.", statusCode: 404);
            }

            _context.Plants.Remove(plant);
            await _context.SaveChangesAsync();

            return CreateResponse(true, "Plant deleted successfully.");
        }

        #endregion

        #region PUT/POST Endpoints

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
                return CreateErrorResponse("No data provided.");
            }

            var plantIds = plantUpdates.Select(p => p.PlantId).Distinct().ToList();
            var plantsToUpdate = await _context.Plants
                .Where(p => plantIds.Contains(p.PlantId))
                .ToListAsync();

            if (plantsToUpdate.Count != plantIds.Count)
            {
                return CreateErrorResponse("One or more plants not found.", statusCode: 404);
            }

            foreach (var update in plantUpdates)
            {
                var plant = plantsToUpdate.First(p => p.PlantId == update.PlantId);
                UpdatePlantEntity(plant, update);
            }

            try
            {
                await _context.SaveChangesAsync();
                return CreateResponse(true, "Plants updated successfully.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error while updating plants.");
                return CreateErrorResponse("A concurrency error occurred while updating the plants.", statusCode: 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating plants.");
                return CreateErrorResponse("An error occurred while updating the plants.", statusCode: 500);
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
                return CreateErrorResponse("No plant data provided.");
            }

            var validationErrors = ValidatePlantCreateDto(model);
            if (validationErrors.Any())
            {
                return CreateErrorResponse("Validation errors occurred.", validationErrors);
            }

            var newPlant = MapToPlant(model);

            if (!TryValidateModel(newPlant))
            {
                var modelErrors = GetModelErrors();
                return CreateErrorResponse("Model state is invalid.", modelErrors);
            }

            _context.Plants.Add(newPlant);

            try
            {
                await _context.SaveChangesAsync();
                return CreateResponse(true, "Plant successfully added!", newPlant);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while adding a new plant.");
                return CreateErrorResponse("An error occurred while adding the plant.", new { error = dbEx.Message }, 500);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding a new plant.");
                return CreateErrorResponse("An unexpected error occurred while adding the plant.", new { error = ex.Message }, 500);
            }
        }

        #endregion

        #region Private Methods

        private void UpdatePlantEntity(Plants plant, PlantUpdateDto update)
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

        private List<string> ValidatePlantCreateDto(PlantCreateDto model)
        {
            var errors = new List<string>();

            if (model.SectorId <= 0)
            {
                errors.Add("Invalid SectorId provided.");
            }

            if (!double.TryParse(model.Latitude?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                errors.Add($"Invalid latitude value: '{model.Latitude}'.");
            }

            if (!double.TryParse(model.Longitude?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                errors.Add($"Invalid longitude value: '{model.Longitude}'.");
            }

            if (model.SectorId == 2 && model.BiometricId == null)
            {
                errors.Add("BiometricId is required for SectorId = 2.");
            }

            return errors;
        }

        private Plants MapToPlant(PlantCreateDto model)
        {
            double.TryParse(model.Latitude?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLatitude);
            double.TryParse(model.Longitude?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLongitude);

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
                Latitude = parsedLatitude,
                Longitude = parsedLongitude,
                HerbariumDuplicate = model.HerbariumDuplicate,
                Note = model.Note,
                YearOfObs = model.YearOfObs,
                PhenophaseDate = model.PhenophaseDate,
                Year = model.Year,
                MeasurementType = model.MeasurementType,
                Value = model.Value,
                BiometricId = model.SectorId == 2 ? model.BiometricId : null
            };
        }

        #endregion
    }
}
