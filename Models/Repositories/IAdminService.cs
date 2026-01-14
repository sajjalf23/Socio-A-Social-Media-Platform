using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocioApp.Services
{
    public interface IAdminService
    {
        Task<List<int>> GetUsersLast7DaysAsync();
        Task<List<int>> GetPostsLast7DaysAsync();
        Task<List<int>> GetCommentsLast7DaysAsync();
        Task<List<int>> GetLikesLast7DaysAsync();
        Task<Dictionary<string, int>> GetTotalCountsAsync(); 
    }
}
