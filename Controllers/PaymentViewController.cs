using Microsoft.AspNetCore.Mvc;

namespace web.Controllers;

public class PaymentViewController : Controller
{
    [HttpGet("/payment")]
    public IActionResult Index() => View("~/Views/Payments/Index.cshtml");

    [HttpGet("/pages/payment")]
    public IActionResult Payment() => View("~/Views/Payments/Index.cshtml");
}
