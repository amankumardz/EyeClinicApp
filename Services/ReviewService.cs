using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;

        public ReviewService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<Review>> GetAdminListAsync() => _context.Reviews
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        public Task<List<Review>> GetApprovedTopAsync(int takeCount) => _context.Reviews
            .AsNoTracking()
            .Where(r => r.IsApproved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(takeCount)
            .ToListAsync();

        public Task<Review?> GetByIdAsync(int id) => _context.Reviews.FindAsync(id).AsTask();

        public async Task CreateAsync(Review review)
        {
            review.CreatedAt = DateTime.UtcNow;
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Review review)
        {
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review is null)
            {
                return;
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();
        }

        public async Task ToggleApprovalAsync(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review is null)
            {
                return;
            }

            review.IsApproved = !review.IsApproved;
            await _context.SaveChangesAsync();
        }
    }
}
