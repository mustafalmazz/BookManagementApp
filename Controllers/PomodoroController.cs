using BookManagementApp.Models;
using BookManagementApp.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization; 

namespace BookManagementApp.Controllers
{
    [Authorize] 
    public class PomodoroController : Controller
    {
        private readonly MyDbContext _context;

        public PomodoroController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveSession([FromBody] StudySessionRequest request)
        {
            try
            {
                if (request.DurationInMinutes < 1)
                {
                    return Json(new { success = false, message = "Geçersiz süre. En az 1 dakikalık çalışmalar kaydedilebilir." });
                }

                var username = User.Identity?.Name;
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username);

                if (currentUser == null)
                {
                    return Json(new { success = false, message = "Oturum bulunamadı. Lütfen giriş yaptığınızdan emin olun." });
                }

                var session = new StudySession
                {
                    UserId = currentUser.Id,
                    SessionType = request.SessionType,
                    DurationInMinutes = request.DurationInMinutes,
                    IsCompleted = request.IsCompleted,
                    CreatedAt = DateTime.Now
                };

                _context.StudySessions.Add(session);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Odaklanma süreniz başarıyla kaydedildi! Harika iş çıkardınız." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Veri kaydedilirken bir hata oluştu: " + ex.Message });
            }
        }
    }

    public class StudySessionRequest
    {
        public string? SessionType { get; set; }
        public int DurationInMinutes { get; set; }
        public bool IsCompleted { get; set; }
    }
}