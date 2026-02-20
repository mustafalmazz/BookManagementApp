using BookManagementApp.Areas.Admin.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookManagementApp.Models
{
    public class StudySession
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }

        // 2. Ne tür bir çalışma? ("Pomodoro", "Stopwatch" (Serbest), "ShortBreak", "LongBreak")
        [Required]
        [MaxLength(50)]
        public string? SessionType { get; set; }

        // 3. İstatistikler için en önemli kısım: Ne kadar sürdü? (Dakika bazında)
        public int DurationInMinutes { get; set; }

        // 4. Aylık/Yıllık filtreleme yapabilmemiz için tarih
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsCompleted { get; set; }
    }
}