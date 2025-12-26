using Microsoft.AspNetCore.Mvc;
using DataManagementApi.Data;
using DataManagementApi.Models;
using Microsoft.EntityFrameworkCore;

namespace DataManagementApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        [HttpPost]
        public async Task<ActionResult<User>> SaveUser(User user)
        {
            var existing = await _context.Users.FindAsync(user.Id);
            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(user);
            }
            else
            {
                // If avatar is null, maybe set default?
                _context.Users.Add(user);
            }
            await _context.SaveChangesAsync();
            return Ok(user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return Ok();
        }
        
        // Update user profile info specifically? Post covers it.
    }
}
