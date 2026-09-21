using CampusFlow.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/quotes")]
public sealed class QuotesController : ControllerBase
{
    // small curated list, picked by day of year so it changes daily but
    // stays the same for everyone on a given day, no database table needed
    private static readonly (string Quote, string? Author)[] Quotes =
    [
        ("Small steps every day add up to big results.", null),
        ("Done is better than perfect.", null),
        ("You do not have to be great to start, but you have to start to be great.", "Zig Ziglar"),
        ("Focus on progress, not perfection.", null),
        ("The secret of getting ahead is getting started.", "Mark Twain"),
        ("Consistency beats intensity.", null),
        ("One task at a time gets the semester done.", null)
    ];

    [HttpGet("daily")]
    [ProducesResponseType<DailyQuoteResponse>(StatusCodes.Status200OK)]
    public ActionResult<DailyQuoteResponse> GetDaily()
    {
        var index = DateTime.UtcNow.DayOfYear % Quotes.Length;
        var (quote, author) = Quotes[index];
        return Ok(new DailyQuoteResponse(quote, author));
    }
}
