using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIshka.Model
{
    [Index(nameof(Login), IsUnique = true)]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Login { get; set; }

        [Required]
        [MaxLength(64)]
        public string PasswordHash { get; set; }
        public string Token { get; set; }
        public DateTime? TokenExpiry { get; set; }

        public int Coins { get; set; } = 0;
        public ICollection<Skin> Skins { get; set; } = new List<Skin>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}