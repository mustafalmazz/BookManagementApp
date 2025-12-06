using BookManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookManagementApp.Areas.Admin.Models
{
    [Index(nameof(UserName), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Kullanıcı adı zorunludur."),Display(Name ="Kullanıcı Adı")]
        [StringLength(50)]
        public string? UserName { get; set; }
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
        public string? Email { get; set; }
        [Required(ErrorMessage = "Şifre zorunludur."), Display(Name = "Şifre")]
        [StringLength(100)]
        public string? PasswordHash { get; set; }
        [Compare("PasswordHash", ErrorMessage = "Şifreler eşleşmiyor."), Display(Name = "Şifre Tekrar")]
        [NotMapped]
        public string? ConfirmPassword { get; set; }
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpires { get; set; }

        public ICollection<Book>? Books { get; set; }
        public ICollection<Category>? Categories { get; set; }
        public ICollection<Contact>? Contacts { get; set; } 
    }
}
