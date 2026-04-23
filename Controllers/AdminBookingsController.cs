using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Security.Claims;


[ApiController]
[Route("api/admin/bookings")]
[Authorize]
public class AdminBookingsController : ControllerBase
{
    private readonly DbConnectionFactory _db;
    private readonly ActivityLogService _logs;

    public AdminBookingsController(
        DbConnectionFactory db,
        ActivityLogService logs)
    {
        _db = db;
        _logs = logs;
    }

    // 1️⃣ Get all bookings
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        using var conn = _db.Create();

        var bookings = await conn.QueryAsync(@"
            SELECT 
                b.id,
                b.booking_date,
                b.status,
                b.created_at
            FROM bookings b
            JOIN escorts e ON e.id = b.escort_id
            ORDER BY b.created_at DESC
        ");

        return Ok(bookings);
    }

    // 2️⃣ Admin override booking status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusDto dto)
    {
        var adminId = Guid.Parse(User.FindFirst("adminId")!.Value);

        using var conn = _db.Create();

        await conn.ExecuteAsync(@"
            UPDATE bookings
            SET status = @Status
            WHERE id = @id
        ", new { id, dto.Status });

        await _logs.LogAsync(
            adminId,
            "UPDATE",
            "booking",
            id,
            $"Booking status set to {dto.Status}"
        );

        return Ok();
    }

    // 3️⃣ Cancel booking
    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var adminId = Guid.Parse(User.FindFirst("adminId")!.Value);

        using var conn = _db.Create();

        await conn.ExecuteAsync(
            "UPDATE bookings SET status = 'cancelled' WHERE id = @id",
            new { id }
        );

        await _logs.LogAsync(
            adminId,
            "CANCEL",
            "booking",
            id,
            "Booking cancelled by admin"
        );

        return Ok();
    }
}
