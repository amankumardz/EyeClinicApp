using EyeClinicApp.Models;

namespace EyeClinicApp.Services
{
    public interface ITeamService
    {
        Task<List<PersonProfile>> GetAdminListAsync();
        Task<List<PersonProfile>> GetActiveProfilesAsync();
        Task<PersonProfile?> GetByIdAsync(int id);
        Task CreateAsync(PersonProfile profile);
        Task UpdateAsync(PersonProfile profile);
        Task DeleteAsync(int id);
        Task ToggleStatusAsync(int id);
    }
}
