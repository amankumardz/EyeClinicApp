using System.Diagnostics;
using EyeClinicApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace EyeClinicApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
