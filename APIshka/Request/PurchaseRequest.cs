using System.ComponentModel.DataAnnotations;

namespace APIshka.Request
{
    public class PurchaseRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int SkinId { get; set; }
    }
}
