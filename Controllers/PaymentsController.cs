using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Services;

namespace web.Controllers;

[ApiController]
[Route("api/payments/simple")]
public class PaymentsController : ApiControllerBase
{
    private readonly PaymentService _paymentService;

    [AllowAnonymous]
    [HttpGet("/payment")]
    public IActionResult Index() => View();

    [AllowAnonymous]
    [HttpGet("/pages/payment")]
    public IActionResult PaymentPage() => View("Index");

    public PaymentsController(PaymentService paymentService, CurrentUserResolver userResolver) : base(userResolver)
    {
        _paymentService = paymentService;
    }

    [HttpPost("{bookingId:int}")]
    public async Task<IResult> Pay([FromRoute] int bookingId)
    {
        var user = await CurrentUserAsync();
        if (user == null) return Results.Unauthorized();
        return ToResult(await _paymentService.SimplePaymentAsync(user, bookingId));
    }
}
