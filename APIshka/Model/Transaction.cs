using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIshka.Model
{
    public class Transaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public int UserId { get; set; }

        [Required]
        [ForeignKey("Skin")]
        public int SkinId { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Skin Skin { get; set; }
    }
}