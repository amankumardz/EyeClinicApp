using EyeClinicApp.Models;

namespace EyeClinicApp.ViewModels
{
    public class HomeIndexViewModel
    {
        public IReadOnlyCollection<Review> TopReviews { get; set; } = Array.Empty<Review>();
    }
}
