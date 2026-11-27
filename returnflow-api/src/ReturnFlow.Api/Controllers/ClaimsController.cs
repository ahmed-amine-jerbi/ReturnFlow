using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReturnFlow.Api.Data;
using ReturnFlow.Api.Domain;
using ReturnFlow.Api.Dtos;
using ReturnFlow.Api.Services;

namespace ReturnFlow.Api.Controllers;

[ApiController]
[Route("api/claims")]
[Authorize]
public class ClaimsController(AppDbContext db) : ControllerBase
{
    /// <summary>Staff list of claims, for the review queue. Filter by processing state or status.</summary>
    [HttpGet]
    [Authorize(Roles = "Agent,Manager,Admin")]
    public async Task<ActionResult<IEnumerable<StaffClaimResponse>>> List(
        [FromQuery] ProcessingState? processing,
        [FromQuery] ClaimStatus? status,
        [FromQuery] int take = 100)
    {
        var query = db.Claims
            .AsNoTracking()
            .Include(c => c.Decisions)
            .Include(c => c.ReturnRequest).ThenInclude(r => r!.Customer)
            .AsQueryable();

        if (processing.HasValue) query = query.Where(c => c.Processing == processing.Value);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync();

        return Ok(items.Select(StaffClaimResponse.From));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClaimResponse>> GetById(int id)
    {
        var claim = await db.Claims
            .AsNoTracking()
            .Include(c => c.Decisions)
            .Include(c => c.ReturnRequest)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim is null) return NotFound();
        if (!User.IsStaff() && claim.ReturnRequest!.CustomerId != User.GetUserId()) return NotFound();

        return Ok(ClaimResponse.From(claim));
    }

    /// <summary>An agent makes the final call on a claim that is waiting in the review queue.</summary>
    [HttpPatch("{id:int}/decision")]
    [Authorize(Roles = "Agent,Manager,Admin")]
    public async Task<ActionResult<ClaimResponse>> Decide(int id, DecisionRequest req)
    {
        var claim = await db.Claims.Include(c => c.Decisions).FirstOrDefaultAsync(c => c.Id == id);
        if (claim is null) return NotFound();

        var agentId = User.GetUserId();

        try
        {
            claim.DecideByAgent(agentId, req.Outcome, req.Comment?.Trim());
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        db.AuditLogs.Add(AuditLog.For(agentId, $"HumanDecision:{req.Outcome}", nameof(ReturnClaim), claim.Id));
        if (!await TrySaveAsync()) return Conflict(new { error = ChangedMeanwhile });

        return Ok(ClaimResponse.From(claim));
    }

    /// <summary>
    /// Human intervention on request: the owner (or staff) asks for a person to look at an automatic decision.
    /// </summary>
    [HttpPost("{id:int}/request-review")]
    public async Task<ActionResult<ClaimResponse>> RequestReview(int id)
    {
        var claim = await db.Claims
            .Include(c => c.Decisions)
            .Include(c => c.ReturnRequest)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (claim is null) return NotFound();

        var userId = User.GetUserId();
        if (!User.IsStaff() && claim.ReturnRequest!.CustomerId != userId) return NotFound();

        try
        {
            claim.RequestHumanReview();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        db.AuditLogs.Add(AuditLog.For(userId, "HumanReviewRequested", nameof(ReturnClaim), claim.Id));
        if (!await TrySaveAsync()) return Conflict(new { error = ChangedMeanwhile });

        return Ok(ClaimResponse.From(claim));
    }

    private const string ChangedMeanwhile = "This claim was changed by someone else. Reload it and try again.";

    /// <summary>False when the claim changed after it was loaded (see <see cref="ReturnClaim.Version"/>).</summary>
    private async Task<bool> TrySaveAsync()
    {
        try
        {
            await db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
