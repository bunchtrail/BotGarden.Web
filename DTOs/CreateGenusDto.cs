using System.ComponentModel.DataAnnotations;

namespace BotGarden.Web.DTOs
{
    public class CreateGenusDto
    {
        [Required(ErrorMessage = "Название рода обязательно")]
        [StringLength(100, ErrorMessage = "Название рода не может быть длиннее 100 символов")]
        public required string GenusName { get; set; }
    }
} 