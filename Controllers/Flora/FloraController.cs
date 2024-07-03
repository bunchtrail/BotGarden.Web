using BotGarden.Application.Services.MainFormAdd;
using BotGarden.Domain.Models;
using BotGarden.Domain.Models.Forms.Dendrology;
using BotGarden.Infrastructure.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BotGarden.Web.Controllers.Flora
{
	public class FloraController : Controller
	{
		private readonly PlantFamilyService _plantFamilyService;
		private readonly BotGardenService _botGardenService;  // Добавляем сервис для местоположений
		private readonly GenusService _genusService;  // Добавляем сервис для местоположений

		private readonly BotanicGardenContext _context;
		public FloraController(PlantFamilyService plantFamilyService, BotGardenService botGardenService, GenusService genusService, BotanicGardenContext context)
		{
			_plantFamilyService = plantFamilyService;
			_botGardenService = botGardenService;
			_genusService = genusService;
			_context = context;
		}

		public async Task<IActionResult> Index()
		{
			var plantFamilies = await _plantFamilyService.GetAllPlantFamiliesAsync();
			var botGardens = await _botGardenService.GetAllBotGardensAsync();
			var genus = await _genusService.GetAllGenusAsync();
			var viewModel = new DendrologyViewModel
			{
				PlantFamilies = plantFamilies,
				BotGardens = botGardens,
				Genuses = genus
			};
			return View(viewModel);
		}


		[HttpPost("api/Flora/Plants/Add")]
        public async Task<IActionResult> AddPlant([FromForm] Plants model, [FromForm] string latitude, [FromForm] string longitude)
        {
            try
            {
                // Замена запятой на точку перед конвертацией строки в double
                latitude = latitude.Replace(',', '.');
                longitude = longitude.Replace(',', '.');

                // Преобразуем строки в double
                if (!double.TryParse(latitude, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedLatitude))
                {
                    return Json(new { success = false, message = "Invalid latitude value", receivedValue = latitude });
                }

                if (!double.TryParse(longitude, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedLongitude))
                {
                    return Json(new { success = false, message = "Invalid longitude value", receivedValue = longitude });
                }



                // Устанавливаем значения широты и долготы в модель вручную перед проверкой модели
                model.Latitude = parsedLatitude;
                model.Longitude = parsedLongitude;
                Console.WriteLine("Model: " + model.Latitude);

                // Удаляем ошибки валидации для полей Latitude и Longitude из ModelState
                ModelState.Remove(nameof(model.Latitude));
                ModelState.Remove(nameof(model.Longitude));

                // Проверяем, чтобы модель была валидной
                if (!ModelState.IsValid)
                {
                    var detailedErrors = ModelState
                        .Where(ms => ms.Value.Errors.Count > 0)
                        .Select(ms => new
                        {
                            Field = ms.Key,
                            Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
                        })
                        .ToList();

                    return Json(new { success = false, message = "Model state is invalid", errors = detailedErrors, model });
                }

                // Создаем новый объект Plants с данными из модели
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
                    Latitude = model.Latitude,  // Используем конвертированные значения
                    Longitude = model.Longitude,  // Используем конвертированные значения
                    HerbariumDuplicate = model.HerbariumDuplicate,
                    Note = model.Note,
                };

                // Добавляем и сохраняем новое растение
                _context.Plants.Add(newPlant);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Plant successfully added!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Internal server error: {ex.Message}", stackTrace = ex.StackTrace });
            }
        }

    }

}

