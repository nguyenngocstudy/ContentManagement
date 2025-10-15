using StackExchange.Redis;

namespace Backend.Auth
{
    public interface IOtpService
    {
        Task<bool> CanSendOtpAsync(string key);
        Task StoreOtpAsync(string key, string otp, TimeSpan expiry);
        Task<bool> VerifyOtpAsync(string key, string otp);
    }
    public class OtpService : IOtpService
    {
        private readonly IDatabase _redis;
        private const int MAX_REQUESTS = 3;
        private readonly TimeSpan WINDOW = TimeSpan.FromMinutes(15);

        public OtpService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }

        public async Task<bool> CanSendOtpAsync(string key)
        {
            var counterKey = $"otp:{key}:count";
            var count = await _redis.StringIncrementAsync(counterKey);

            if (count == 1)
                await _redis.KeyExpireAsync(counterKey, WINDOW);

            return count <= MAX_REQUESTS;
        }

        public async Task StoreOtpAsync(string key, string otp, TimeSpan expiry)
        {
            await _redis.StringSetAsync($"otp:{key}:code", otp, expiry);
        }

        public async Task<bool> VerifyOtpAsync(string key, string otp)
        {
            var stored = await _redis.StringGetAsync($"otp:{key}:code");
            return stored == otp;
        }
    }

}
