using BotGarden.Application.DTOs;
using BotGarden.Infrastructure.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


public class FloricultureAllController : Controller
{
	private readonly BotanicGardenContext _context;

	public FloricultureAllController(BotanicGardenContext context)
	{
		_context = context;
	}

	public IActionResult Index()
	{
		var plants = _context.Plants
			.Include(p => p.Family)
			.Include(p => p.Genus)
			.Include(p => p.Sector)
			.ToList();
		return View(plants);
	}

	[HttpDelete("api/floriculture/delete/{id}")]
	public async Task<IActionResult> Delete(int id)
	{
		var plant = await _context.Plants.FindAsync(id);
		if (plant == null)
		{
			return Json(new { success = false, message = "Plant not found." });
		}

		_context.Plants.Remove(plant);
		await _context.SaveChangesAsync();

		return Json(new { success = true, message = "Plant deleted successfully." });
	}


	[HttpPost("api/floriculture/update")]
	public async Task<IActionResult> UpdatePlants([FromBody] List<PlantUpdateDto> plantUpdates)
	{
		if (plantUpdates == null || !plantUpdates.Any())
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

				// Обновляем булевы значения, которые не могут быть null в вашем DTO, учитывая, что они могут быть опциональными
				if (update.HerbariumPresence.HasValue)
					plant.HerbariumPresence = update.HerbariumPresence.Value;

				plant.Note = update.Note ?? plant.Note;
			}
		}

		await _context.SaveChangesAsync();


		await _context.SaveChangesAsync();
		return Ok(new { success = true, message = "Plants updated successfully." });
	}



}
