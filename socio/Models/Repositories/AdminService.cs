using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SocioApp.Data;

namespace SocioApp.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _db;

        public AdminService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<int>> GetUsersLast7DaysAsync()
        {
            var result = new List<int>();
            var today = DateTime.UtcNow.Date;
            
            // Get all user creation dates
            var userDates = await _db.Users
                .Select(u => EF.Property<DateTime>(u, "CreatedAt").Date)
                .Where(date => date >= today.AddDays(-6))
                .ToListAsync();

            for (int i = 6; i >= 0; i--)
            {
                var day = today.AddDays(-i);
                var count = userDates.Count(d => d == day);
                result.Add(count);
            }
            
            return result;
        }

        public async Task<List<int>> GetPostsLast7DaysAsync()
        {
            var result = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _db.Posts
                    .Where(p => p.CreatedAt.Date == day)
                    .CountAsync();
                result.Add(count);
            }
            return result;
        }

        public async Task<List<int>> GetCommentsLast7DaysAsync()
        {
            var result = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _db.Comments
                    .Where(c => c.CreatedAt.Date == day)
                    .CountAsync();
                result.Add(count);
            }
            return result;
        }

        public async Task<List<int>> GetLikesLast7DaysAsync()
        {
            var result = new List<int>();
            for (int i = 6; i >= 0; i--)
            {
                var day = DateTime.UtcNow.Date.AddDays(-i);
                var count = await _db.PostReactions
                    .Where(l => l.CreatedAt.Date == day)
                    .CountAsync();
                result.Add(count);
            }
            return result;
        }

        // Optional: Add total counts for cards
        public async Task<Dictionary<string, int>> GetTotalCountsAsync()
        {
            return new Dictionary<string, int>
            {
                ["TotalUsers"] = await _db.Users.CountAsync(),
                ["TotalPosts"] = await _db.Posts.CountAsync(),
                ["TotalComments"] = await _db.Comments.CountAsync(),
                ["TotalLikes"] = await _db.PostReactions.CountAsync()
            };
        }
    }
}