using BookManagementApp.Models;
using System.ComponentModel.DataAnnotations;

namespace BookManagementApp.Areas.Admin.Models
{
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
         
        public ICollection<Book>? Books { get; set; }
        public ICollection<Category>? Categories { get; set; }
    }
}
