using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Dtos;
using ReturnFlow.Api.Services;

namespace ReturnFlow.Api.Controllers;

[ApiController]
[Route("api/returns")]
[Authorize]
public class ReturnsController(AppDbContext db, ClaimQueue queue) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = nameof(Role.Customer))]
    public async Task<ActionResult<ReturnResponse>> Create(CreateReturnRequest req)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (req.PurchaseDate > today)
            return BadRequest(new { error = "Purchase date cannot be in the future." });
        if (req.PurchaseDate < today.AddYears(-1))
            return BadRequest(new { error = "Purchase date is more than a year ago." });

        var userId = User.GetUserId();

        var entity = new ReturnRequest
        {
            CustomerId = userId,
            OrderReference = req.OrderReference.Trim(),
            PurchaseDate = req.PurchaseDate,
            Reason = req.Reason.Trim()
        };
        await db.AddWithAuditAsync(entity, r => AuditLog.For(userId, "ReturnCreated", nameof(ReturnRequest), r.Id));

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, ReturnResponse.From(entity));
    }

    /// <summary>Customers see their own returns; staff see all returns.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReturnResponse>>> List()
    {
        var query = db.Returns
            .AsNoTracking()
            .Include(r => r.Claims).ThenInclude(c => c.Decisions)
            .AsQueryable();

        if (!User.IsStaff())
        {
            var userId = User.GetUserId();
            query = query.Where(r => r.CustomerId == userId);
        }

        var items = await query.OrderByDescending(r => r.CreatedAt).Take(200).ToListAsync();
        return Ok(items.Select(ReturnResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ReturnResponse>> GetById(int id)
    {
        var entity = await db.Returns
            .AsNoTracking()
            .Include(r => r.Claims).ThenInclude(c => c.Decisions)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (entity is null) return NotFound();
        if (!User.IsStaff() && entity.CustomerId != User.GetUserId()) return NotFound();

        return Ok(ReturnResponse.From(entity));
    }

    [HttpPost("{id:int}/claims")]
    [Authorize(Roles = nameof(Role.Customer))]
    public async Task<ActionResult<ClaimResponse>> AddClaim(int id, CreateClaimRequest req)
    {
        var userId = User.GetUserId();

        var parent = await db.Returns.FirstOrDefaultAsync(r => r.Id == id && r.CustomerId == userId);
        if (parent is null) return NotFound();

        var claim = new ReturnClaim
        {
            ReturnRequestId = parent.Id,
            Description = req.Description.Trim(),
            AmountRequested = req.AmountRequested,
            Processing = ProcessingState.Analyzing
        };
        await db.AddWithAuditAsync(claim, c => AuditLog.For(userId, "ClaimCreated", nameof(ReturnClaim), c.Id));

        // Hand over to the background worker: the AI analysis does not block this request.
        queue.Enqueue(claim.Id);

        return CreatedAtAction("GetById", "Claims", new { id = claim.Id }, ClaimResponse.From(claim));
    }
}
