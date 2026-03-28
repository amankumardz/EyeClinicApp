using System.Diagnostics;
using EyeClinicApp.Models;
using EyeClinicApp.Services;
using EyeClinicApp.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace EyeClinicApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IReviewService _reviewService;

        public HomeController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new HomeIndexViewModel
            {
                TopReviews = await _reviewService.GetApprovedTopAsync(3)
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
