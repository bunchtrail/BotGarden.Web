using Microsoft.AspNetCore.Mvc;
using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Infrastructure.Contexts;
using BotGarden.Domain.Models;
using BotGarden.Domain.Models.Forms.Dendrology;
using System.Globalization;


namespace BotGarden.Web.Controllers.Dendrology
{
    [Route("api/dendrology")]
    [ApiController]
    public class DendrologyController : ControllerBase
    {
        private readonly PlantFamilyService _plantFamilyService;
        private readonly BotGardenService _botGardenService;
        private readonly GenusService _genusService;
        private readonly BotanicGardenContext _context;

        public DendrologyController(PlantFamilyService plantFamilyService, BotGardenService botGardenService, GenusService genusService, BotanicGardenContext context)
        {
            _plantFamilyService = plantFamilyService;
            _botGardenService = botGardenService;
            _genusService = genusService;
            _context = context;
        }

        [HttpGet("index")]
        public async Task<IActionResult> Index()
        {
            var plantFamilies = await _plantFamilyService.GetAllPlantFamiliesAsync();
            var botGardens = await _botGardenService.GetAllBotGardensAsync();
            var genuses = await _genusService.GetAllGenusAsync();

            var viewModel = new DendrologyAllViewModel
            {
                PlantFamilies = plantFamilies,
                BotGardens = botGardens,
                Genuses = genuses
            };

            return Ok(viewModel);
        }

        [HttpGet("word")]
        public async Task<IActionResult> Word()
        {
            var plantFamilies = await _plantFamilyService.GetAllPlantFamiliesAsync();
            var botGardens = await _botGardenService.GetAllBotGardensAsync();
            var genuses = await _genusService.GetAllGenusAsync();

            var viewModel = new DendrologyAllViewModel
            {
                PlantFamilies = plantFamilies,
                BotGardens = botGardens,
                Genuses = genuses
            };

            return Ok(viewModel);
        }

        [HttpPost("plants/add")]
        public async Task<IActionResult> AddPlant([FromForm] Plants model, [FromForm] string latitude, [FromForm] string longitude)
        {
            try
            {
                if (!TryParseCoordinates(latitude, out double parsedLatitude))
                {
                    return BadRequest(new { success = false, message = "Invalid latitude value", receivedValue = latitude });
                }

                if (!TryParseCoordinates(longitude, out double parsedLongitude))
                {
                    return BadRequest(new { success = false, message = "Invalid longitude value", receivedValue = longitude });
                }

                model.Latitude = parsedLatitude;
                model.Longitude = parsedLongitude;
                Console.WriteLine($"Model: Latitude = {model.Latitude}, Longitude = {model.Longitude}");

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
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Internal server error: {ex.Message}", stackTrace = ex.StackTrace });
            }
        }

        private bool TryParseCoordinates(string value, out double result)
        {
            value = value.Replace(',', '.');
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
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
