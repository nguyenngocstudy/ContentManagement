using Backend.Auth;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;
namespace Backend.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IOtpService _otpService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        /// <summary>
        /// Đăng ký người dùng mới
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var token = await _authService.Register(dto);
            return Ok(new { token });
        }

        /// <summary>
        /// Đăng nhập người dùng
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var token = await _authService.Login(dto);
            // Lưu token vào cookie
            Response.Cookies.Append("jwt_token", token, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddDays(1), // Lưu phiên trong 1 ngày
                Secure = true, // Chỉ gửi qua HTTPS, bỏ nếu dev local HTTP
                SameSite = SameSiteMode.Strict // Tùy chỉnh theo nhu cầu
            });

            return Ok(new { message = "Login successful" });
        }
        /// <summary>
        /// Gửi mã OTP qua email hoặc SMS
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpDto dto)
        {
            var otp = _authService.GenerateOtp();

            if (!string.IsNullOrEmpty(dto.Email))
            {
                await _authService.SendOtpEmail(dto.Email, otp);
            }
            else if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                _authService.SendOtpSms(dto.PhoneNumber, otp);
            }
            else
            {
                return BadRequest("Email hoặc số điện thoại là bắt buộc.");
            }

            // Lưu OTP vào DB hoặc cache
            //await _otpService.SaveOtp(dto.Email ?? dto.PhoneNumber, otp);

            return Ok(new { message = "OTP sent successfully" });
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendOtpp([FromBody] SendOtpDto dto)
        {
            var key = dto.Email ?? dto.PhoneNumber;

            if (!await _otpService.CanSendOtpAsync(key))
                return BadRequest("Bạn đã gửi quá số lần cho phép. Vui lòng thử lại sau.");

            var otp = new Random().Next(100000, 999999).ToString();
            await _otpService.StoreOtpAsync(key, otp, TimeSpan.FromMinutes(5));

            // Gửi qua email hoặc SMS ở đây (MailKit/Twilio...)

            return Ok(new { message = "Mã xác thực đã được gửi." });
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var key = dto.Email ?? dto.PhoneNumber;

            if (await _otpService.VerifyOtpAsync(key, dto.Otp))
            {
                return Ok(new { message = "Xác minh thành công" });
            }

            return BadRequest("Mã xác thực không đúng hoặc đã hết hạn.");
        }
    }
}
