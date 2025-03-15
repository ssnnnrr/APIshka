using System.ComponentModel.DataAnnotations;

namespace APIshka.Request
{
    public class RegisterRequest
    {
        [Required]
        [MaxLength(50)]
        public string Login { get; set; }

        [Required]
        [MaxLength(64)]
        public string Password { get; set; }
    }
}
