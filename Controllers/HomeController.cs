using Microsoft.AspNetCore.Mvc;

namespace SmartELibrary.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString("Role");
        return role switch
        {
            "Admin" => RedirectToAction("Dashboard", "Admin"),
            "Teacher" => RedirectToAction("Dashboard", "Teacher"),
            "Student" => RedirectToAction("Dashboard", "Student"),
            _ => View()
        };
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Contact()
    {
        return View();
    }

    public IActionResult Help()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
