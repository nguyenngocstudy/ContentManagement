using System.Runtime.CompilerServices;

namespace Backend.Model
{
    public class RegisterDto {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class LoginDto {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class SendOtpDto
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class VerifyOtpDto
    {
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Otp { get; set; } = string.Empty;
    }
    public class SmtpOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}