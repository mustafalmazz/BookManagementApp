using BookManagementApp.Areas.Admin.Models;
using BookManagementApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;

namespace BookManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;

        public AccountController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                var usernameCheck = _context.Users.FirstOrDefault(u => u.UserName == user.UserName);
                if (usernameCheck != null)
                {
                    ModelState.AddModelError("UserName", "Bu kullanıcı adı zaten alınmış!");
                    return View(user);
                }

                var emailCheck = _context.Users.FirstOrDefault(u => u.Email == user.Email);
                if (emailCheck != null)
                {
                    ModelState.AddModelError("Email", "Bu email adresi ile zaten kayıtlı bir hesap var!");
                    return View(user);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

                _context.Users.Add(user);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }
            return View(user);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == username);

            if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                await SignInUserAsync(user);
                if (user.Role == "SuperAdmin")
                {
                    // Eğer SuperAdmin ise özel panele git
                    return RedirectToAction("Index", "DashBoard", new { area = "SuperAdmin" });
                }
            }

            ViewBag.Error = "Geçersiz kullanıcı adı veya şifre.";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }

        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse")
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> GoogleResponse()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded) return RedirectToAction("Login");

            var claims = result.Principal.Identities.FirstOrDefault().Claims;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user != null)
            {
                await SignInUserAsync(user);
                if (user.Role == "SuperAdmin")
                {
                    return RedirectToAction("Index", "DashBoard", new { area = "SuperAdmin" });
                }
            }

            TempData["GoogleEmail"] = email;
            return RedirectToAction("GoogleRegister");
        }

        [HttpGet]
        public IActionResult GoogleRegister()
        {
            if (TempData["GoogleEmail"] == null)
            {
                return RedirectToAction("Login");
            }

            var email = TempData["GoogleEmail"].ToString();
            TempData.Keep("GoogleEmail");

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GoogleRegister(string username)
        {
            if (TempData["GoogleEmail"] == null)
            {
                return RedirectToAction("GoogleLogin");
            }

            string email = TempData["GoogleEmail"].ToString();

            if (string.IsNullOrWhiteSpace(username))
            {
                ViewBag.Error = "Lütfen bir kullanıcı adı belirleyin.";
                ViewBag.Email = email;
                TempData.Keep("GoogleEmail");
                return View();
            }

            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName.ToLower() == username.ToLower());
            if (existingUser != null)
            {
                ViewBag.Error = "Bu kullanıcı adı zaten kullanımda, lütfen başka bir tane seçin.";
                ViewBag.Email = email;
                TempData.Keep("GoogleEmail");
                return View();
            }

            var newUser = new User
            {
                UserName = username,
                Email = email,
                PasswordHash = Guid.NewGuid().ToString()
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            await SignInUserAsync(newUser);

            return RedirectToAction("Index", "Home");
        }

        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, user.UserName ?? ""),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        // ESKİ HALİ: new Claim(ClaimTypes.Role, "User") 
        // YENİ HALİ: Veritabanındaki rolü al, boşsa "User" kabul et
        new Claim(ClaimTypes.Role, user.Role ?? "User")
    };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.UserName ?? "");
        }
        // --- ŞİFREMİ UNUTTUM BÖLÜMÜ ---

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                ViewBag.Error = "Bu e-posta adresiyle kayıtlı kullanıcı bulunamadı.";
                return View();
            }

            // Token Oluştur (Rastgele güvenli bir kod)
            string token = Guid.NewGuid().ToString();

            // Token veritabanına kaydet ve 1 saat süre ver
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpires = DateTime.Now.AddHours(1);
            await _context.SaveChangesAsync();

            // Link Oluştur: https://site.com/Account/ResetPassword?token=...&email=...
            var resetLink = Url.Action("ResetPassword", "Account", new { token = token, email = email }, Request.Scheme);

            // Mail Gönder
            string subject = "Şifre Sıfırlama İsteği";
            string body = $"Merhaba,<br><br>Şifrenizi sıfırlamak için lütfen aşağıdaki linke tıklayın:<br><br><a href='{resetLink}'>Şifremi Sıfırla</a>";

            try
            {
                SendEmail(email, subject, body);
                TempData["Success"] = "Sıfırlama linki e-posta adresinize gönderildi.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Mail gönderilirken bir hata oluştu: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (token == null || email == null)
            {
                ViewBag.Error = "Geçersiz link.";
                return View("Login");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpires < DateTime.Now)
            {
                ViewBag.Error = "Geçersiz veya süresi dolmuş link.";
                return View("Login");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Şifreler eşleşmiyor.";
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.PasswordResetToken != token || user.PasswordResetTokenExpires < DateTime.Now)
            {
                ViewBag.Error = "İşlem başarısız. Linkin süresi dolmuş olabilir.";
                return View("Login");
            }

            // Yeni şifreyi kaydet ve token'ı temizle
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Şifreniz başarıyla değiştirildi. Giriş yapabilirsiniz.";
            return RedirectToAction("Login");
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("mustafagfb19077091@gmail.com", "jywirtxtsnpphyto"),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("mustafagfb19077091@gmail.com", "Dijital Kütüphanem"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }
    }
}