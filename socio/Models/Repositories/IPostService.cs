using SocioApp.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPostService 
{
    Task<int> CreatePostAsync(string userId, string caption, IFormFile? imageFile);

    Task<bool> EditPostAsync(int postId, string userId, string caption, IFormFile? imageFile);
    Task<Post?> GetPostByIdAsync(int postId);

    Task<bool> DeletePostAsync(int postId, string userId);

    Task<bool> ToggleHidePostAsync(int postId);

    Task<IEnumerable<Post>> GetAllPostsAsync(bool includeHidden = false);

    Task<bool> LikePostAsync(int postId);
    Task<bool> DislikePostAsync(int postId);

}

