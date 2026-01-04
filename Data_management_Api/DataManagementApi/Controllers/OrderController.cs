using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataManagementApi.Data;
using DataManagementApi.Models;

namespace DataManagementApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Order/kmart?year=2025&month=11&endMonth=12
        [HttpGet("kmart")]
        public async Task<ActionResult<IEnumerable<KmartDailyRecord>>> GetKmartRecords([FromQuery] int year, [FromQuery] int? month, [FromQuery] int? endMonth)
        {
            var query = _context.KmartDailyRecords.Where(r => r.Date.Year == year);

            if (endMonth.HasValue)
            {
                // Year-to-date range: 1 to endMonth
                query = query.Where(r => r.Date.Month >= 1 && r.Date.Month <= endMonth.Value);
            }
            else if (month.HasValue)
            {
                // Single month
                query = query.Where(r => r.Date.Month == month.Value);
            }

            var records = await query.ToListAsync();
            return Ok(records);
        }

        // POST: api/Order/kmart
        [HttpPost("kmart")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<ActionResult> SaveKmartRecords([FromBody] List<KmartDailyRecord> records)
        {
            if (records == null || !records.Any())
            {
                return BadRequest("No records provided");
            }

            var username = User.Identity?.Name ?? "Unknown";

            foreach (var record in records)
            {
                // Normalize date to remove time components for uniqueness check
                var normalizedDate = record.Date.Date;

                // Uniqueness check: Location + Category + SubCategory + Date
                var existing = await _context.KmartDailyRecords
                    .FirstOrDefaultAsync(r => 
                        r.Location == record.Location &&
                        r.Category == record.Category &&
                        (r.SubCategory ?? "") == (record.SubCategory ?? "") &&
                        r.Date.Date == normalizedDate);

                if (existing != null)
                {
                    existing.Quantity = record.Quantity;
                    existing.ModifiedBy = username;
                    existing.UpdatedAt = DateTime.Now;
                }
                else
                {
                    record.Date = normalizedDate; 
                    record.ModifiedBy = username;
                    record.UpdatedAt = DateTime.Now;
                    _context.KmartDailyRecords.Add(record);
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Records saved successfully" });
        }
    }
}
