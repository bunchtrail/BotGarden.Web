using System.ComponentModel.DataAnnotations;

namespace BotGarden.Web.DTOs
{
    public class CreateSectorDto
    {
        [Required(ErrorMessage = "Название сектора обязательно")]
        [StringLength(100, ErrorMessage = "Название сектора не может быть длиннее 100 символов")]
        public required string SectorName { get; set; }
    }
} 