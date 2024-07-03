using BotGarden.Domain.Models;

namespace BotGarden.Domain.Models.Forms
{
	public class PlantsViewModel
	{
		public IEnumerable<Plants>? Plants { get; set; }
		public IEnumerable<Collections>? Collections { get; set; }
	}
}
