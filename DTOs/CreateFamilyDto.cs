using System.ComponentModel.DataAnnotations;

namespace BotGarden.Web.DTOs
{
    public class CreateFamilyDto
    {
        [Required(ErrorMessage = "Название семейства обязательно")]
        [StringLength(100, ErrorMessage = "Название семейства не может быть длиннее 100 символов")]
        public required string FamilyName { get; set; }
    }
} 