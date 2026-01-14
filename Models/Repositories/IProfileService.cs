using SocioApp.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocioApp.Services
{
    public interface IProfileService
    { 
        Task<ProfileViewModel?> GetProfileAsync(string userId);
        Task<bool> ToggleBanAsync(string userId);
        Task<IEnumerable<ProfileViewModel>> GetAllUsersAsync();
        Task<ProfileViewModel?> GetProfilebyidFeed(string a);

        Task<IEnumerable<ProfileViewModel>> SearchUsersAsync(string searchTerm);
        public Task<ProfileViewModel?> GetProfilebyidAsync(string b);

        Task<IEnumerable<ProfileViewModel>> GetAllUserforadmin();
        public Task<bool> UpdateProfileAsync(string userId, string username, string name, string bio, IFormFile? profileImageFile, string preprofileimg);
    }
}
