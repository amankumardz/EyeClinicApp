using EyeClinicApp.Data;
using EyeClinicApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EyeClinicApp.Services
{
    public class TeamService : ITeamService
    {
        private readonly ApplicationDbContext _context;

        public TeamService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<List<PersonProfile>> GetAdminListAsync() => _context.PersonProfiles
            .AsNoTracking()
            .OrderBy(p => p.Role)
            .ThenBy(p => p.Name)
            .ToListAsync();

        public Task<List<PersonProfile>> GetActiveProfilesAsync() => _context.PersonProfiles
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Role)
            .ThenBy(p => p.Name)
            .ToListAsync();

        public Task<PersonProfile?> GetByIdAsync(int id) => _context.PersonProfiles.FindAsync(id).AsTask();

        public async Task CreateAsync(PersonProfile profile)
        {
            profile.CreatedAt = DateTime.UtcNow;
            _context.PersonProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(PersonProfile profile)
        {
            _context.PersonProfiles.Update(profile);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var profile = await _context.PersonProfiles.FindAsync(id);
            if (profile is null)
            {
                return;
            }

            _context.PersonProfiles.Remove(profile);
            await _context.SaveChangesAsync();
        }

        public async Task ToggleStatusAsync(int id)
        {
            var profile = await _context.PersonProfiles.FindAsync(id);
            if (profile is null)
            {
                return;
            }

            profile.IsActive = !profile.IsActive;
            await _context.SaveChangesAsync();
        }
    }
}
