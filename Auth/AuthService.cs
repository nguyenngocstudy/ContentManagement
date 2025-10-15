using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Model.Context;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
namespace Backend.Auth
{
    public interface IAuthService
    {
        Task<string> Register(RegisterDto dto);
        Task<string> Login(LoginDto dto);

        Task SendOtpEmail(string email, string otp);

        void SendOtpSms(string phoneNumber, string otp);

        string GenerateOtp(int length = 6);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;


        public AuthService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // Đăng ký
        public async Task<string> Register(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.username == dto.Username))
                throw new Exception("User already exists");

            var user = new Users
            {
                username = dto.Username,
                password_hash = Convert.FromBase64String(dto.Password), // Password là SHA256 dạng base64
                role = "User"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreateToken(user);
        }

        // Đăng nhập

        public async Task<string> Login(LoginDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.username == dto.Username);
            if (user == null || !user.password_hash.SequenceEqual(Convert.FromBase64String(dto.Password)))
                throw new Exception("Invalid credentials");

            return CreateToken(user);
        }

        private void CreatePasswordHash(string password, out byte[] hash, out byte[] salt)
        {
            using (var hmac = new HMACSHA512())
            {
                salt = hmac.Key;
                hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPasswordHash(string password, byte[] hash, byte[] salt)
        {
            using (var hmac = new HMACSHA512(salt))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(hash);
            }
        }

        private string CreateToken(Users user)
        {
            var claims = new[] {
                new Claim(ClaimTypes.Name, user.username),
                new Claim(ClaimTypes.Role, user.role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        /// <summary>
        /// Lấy otp gửi mail
        /// </summary>
        /// <param name="email"></param>
        /// <param name="otp"></param>
        /// <returns></returns>
        public async Task SendOtpEmail(string email, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config["Smtp: Username"]));
            message.To.Add(MailboxAddress.Parse(email));    
            message.Subject = "Mã xác thực đăng ký";

            message.Body = new TextPart("plain")
            {
                Text = $"Mã xác thực của bạn là: {otp}"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_config["Smtp: Host"], int.Parse(_config["Smtp: Port"]), MailKit.Security.SecureSocketOptions.StartTls);


            await client.AuthenticateAsync(_config["Smtp: Username"], _config["Smtp: Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        /// <summary>
        /// Lấy otp gửi SMS qua Twilio
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="otp"></param>
        public void SendOtpSms(string phoneNumber, string otp)
        {
            const string accountSid = "YOUR_TWILIO_SID";
            const string authToken = "YOUR_TWILIO_AUTH_TOKEN";

            TwilioClient.Init(accountSid, authToken);

            var message = MessageResource.Create(
                body: $"Mã xác thực của bạn là: {otp}",
                from: new Twilio.Types.PhoneNumber("+1234567890"), // số từ Twilio
                to: new Twilio.Types.PhoneNumber(phoneNumber)
            );
        }

        /// <summary>
        /// Tạo mã OTP ngẫu nhiên
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public string GenerateOtp(int length = 6)
        {
            var rng = new Random();
            return string.Join("", Enumerable.Range(0, length).Select(_ => rng.Next(0, 10)));
        }
    }
}
