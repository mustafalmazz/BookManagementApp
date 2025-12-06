using System.ComponentModel.DataAnnotations;

namespace BookManagementApp.Areas.Admin.Models
{
    public class Contact
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Konu başlığı gereklidir.")]
        [StringLength(100)]
        public string? Subject { get; set; }
        [Required(ErrorMessage = "Mesaj içeriği gereklidir.")]
        public string? Message { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? GuestName { get; set; }
        public string? GuestEmail { get; set; }
        public int? UserId { get; set; }
        public User? User { get; set; }

    }
}
