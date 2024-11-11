using CodeBridgeTestTask.Contexts;
using CodeBridgeTestTask.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CodeBridgeTestTask.Controllers
{
    [EnableRateLimiting("fixed")]
    public class DogController : Controller
    {
        private readonly DogContext _context;

        public DogController(DogContext context)
        {
            _context = context;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Dogshouseservice.Version1.0.1");
        }

        [HttpGet("dogs")]
        public async Task<IActionResult> GetDogs([FromQuery] string attribute = "name", [FromQuery] string order = "asc", [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = _context.Dogs.AsQueryable();

            query = attribute.ToLower() switch
            {
                "weight" => order.ToLower() == "desc" ? query.OrderByDescending(d => d.Weight) : query.OrderBy(d => d.Weight),
                "tail_length" => order.ToLower() == "desc" ? query.OrderByDescending(d => d.TailLength) : query.OrderBy(d => d.TailLength),
                _ => order.ToLower() == "desc" ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name)
            };

            var paginatedDogs = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
            Console.WriteLine(paginatedDogs.GetRange(0, paginatedDogs.Count));
            return Ok(paginatedDogs);
        }

        [HttpPost("dog")]
        public async Task<IActionResult> CreateDog([FromBody] Dog dog)
        {
            if (await _context.Dogs.AnyAsync(d => d.Name == dog.Name))
                return BadRequest("A dog with this name already exists.");

            if (dog.TailLength < 0)
                return BadRequest("Tail length must be a positive number.");

            if (dog.Weight <= 0)
                return BadRequest("Weight must be a positive number.");

            _context.Dogs.Add(dog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetDogs), new { name = dog.Name }, dog);
        }
    }
}

