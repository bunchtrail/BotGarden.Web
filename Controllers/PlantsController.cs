using Microsoft.AspNetCore.Mvc;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BotGarden.Infrastructure.Contexts;

public class PlantsController : Controller
{
    private readonly BotanicGardenContext _context;

    public PlantsController(BotanicGardenContext context)
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

}
