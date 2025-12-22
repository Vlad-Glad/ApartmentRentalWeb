using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentRental.Controllers;

[Authorize]
public sealed class DemoController : Controller
{
    [HttpGet]
    public IActionResult Realtime()
    {
        ViewData["Title"] = "Realtime Demo";
        return View();
    }
}
