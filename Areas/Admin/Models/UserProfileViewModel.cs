namespace BookManagementApp.Areas.Admin.Models
{
    public class UserProfileViewModel
    {
        public string? UserName { get; set; }
        public string? Role { get; set; }
        public DateTime JoinDate { get; set; }

        // Temel İstatistikler
        public int TotalBooks { get; set; }
        public int TotalCategories { get; set; }
        public int TotalPagesRead { get; set; }
        public decimal TotalMoneySpent { get; set; }
        public string? ProfileImageUrl { get; set; }
        

        // Ekstra Özellikler
        public string? FavoriteCategory { get; set; } // En çok kitabı olan kategori
        public int YearlyReadingGoal { get; set; }  // Örn: Yıllık hedef (sabit veya db'den)
        public int BooksReadThisYear { get; set; } // Bu yıl okunanlar
        public User? User { get; set; }
    }
}
