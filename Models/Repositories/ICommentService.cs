using SocioApp.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ICommentService
{
    Task<Comment> AddCommentAsync(Comment comment);
    Task<List<Comment>> GetCommentsAsync(int postId);

    Task<IEnumerable<Comment>> GetCommentsByUserAsync(string userId);
    Task<bool> DeleteCommentAsync(int commentId, string userId);
    Task<bool> ToggleHideCommentAsync(int commentId);
}
