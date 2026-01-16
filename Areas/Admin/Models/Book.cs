using BookManagementApp.Areas.Admin.Models;
using System.ComponentModel.DataAnnotations;

namespace BookManagementApp.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kitap adı zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Kitap Adı")]
        public string Name { get; set; } = string.Empty;
        [Display(Name = "Resim")]
        public string? Image {  get; set; }
        [Display(Name = "Sayfa Sayısı")]
        public int? TotalPages { get; set; }
        [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasında olmalı."),Display(Name="Puan")]
        public decimal? Rate { get; set; }

        [Required(ErrorMessage = "Yazar adı zorunludur.")]
        [StringLength(200)]
        [Display(Name = "Yazar Adı")]
        public string Author { get; set; } = string.Empty;

        [StringLength(1600)]
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }
        [Display(Name = "Kitap Notlarım")]
        public string? Notes { get; set; }


        [Range(0, 9999.99, ErrorMessage = "Fiyat 0 ile 9999 arasında olmalı.")]
        [Display(Name = "Fiyat")]
        public decimal? Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stok sayısı negatif olamaz.")]
        [Display(Name = "Adet")]
        public int Stock { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Tarih")]
        public DateTime CreateDate { get; set; } = DateTime.Now;
        [Display(Name = "Kategori")]
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        [Display(Name = "Kullanıcı")]
        public int? UserId { get; set; }
        public User? User { get; set; }
    }
}
