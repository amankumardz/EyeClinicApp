using EyeClinicApp.Models;

namespace EyeClinicApp.Services
{
    public interface IReviewService
    {
        Task<List<Review>> GetAdminListAsync();
        Task<List<Review>> GetApprovedTopAsync(int takeCount);
        Task<Review?> GetByIdAsync(int id);
        Task CreateAsync(Review review);
        Task UpdateAsync(Review review);
        Task DeleteAsync(int id);
        Task ToggleApprovalAsync(int id);
    }
}
