using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using RedisCacheSystem.Entities;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;

namespace RedisCacheSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RedisController : ControllerBase
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        public RedisController(IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _cache = cache;
            _redis = redis;
        }



        [HttpPost]
        public async Task<IActionResult> Write(Redis request)
        {
            var content = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));

            await _cache.SetAsync("Redis_" + request.Key, content, new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(1) });
            
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Read(string key)
        {
            var content = await _cache.GetStringAsync($"Redis_{key}");

            if (content == null)
                return NotFound("This key is not available.");

            return Ok(JsonSerializer.Deserialize<Redis>(content));
        }



        [HttpGet("GetAll")]
        public async Task<IActionResult> GetAll()
        {
            var redisKeys = _redis.GetServer("localhost", 9191).Keys(pattern: "Redis_*")
                .AsQueryable().Select(p => p.ToString()).ToList();

            var result = new List<Redis>();

            foreach (var redisKey in redisKeys)
            {
                result.Add(JsonSerializer.Deserialize<Redis>(await _cache.GetStringAsync(redisKey)));
            }

            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(string key)
        {
            await _cache.RemoveAsync($"Redis_{key}");

            return Ok();
        }
    }
}
