using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SocioApp.Data;
using SocioApp.Models;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CommentService> _logger;

     private readonly IConfiguration _configuration;

    public CommentService(ApplicationDbContext context, ILogger<CommentService> logger, IConfiguration config)
    {
        _context = context;
        _logger = logger;
        _configuration = config;
    }

    // Get SQL connection from ApplicationDbContext for Dapper
    private SqlConnection GetConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Database connection string is not configured.");

        return new SqlConnection(connectionString);
    }

    public async Task<List<Comment>> GetCommentsAsync(int postId)
    {
        try
        {
            using var connection = GetConnection();
            var sql = @"
            SELECT 
                c.CommentId, c.PostId, c.UserId, c.Content, c.IsHidden, c.CreatedAt, c.UpdatedAt,
                u.UserName AS CommentUserName
            FROM Comments c
            INNER JOIN AspNetUsers u ON c.UserId = u.Id
            WHERE c.PostId = @PostId AND c.IsHidden = 0
            ORDER BY c.CreatedAt ASC;";

            var comments = await connection.QueryAsync<Comment>(sql, new { PostId = postId });
            return comments.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments for post {PostId}", postId);
            throw;
        }
    }
    public async Task<IEnumerable<Comment>> GetCommentsByUserAsync(string userId)
    {
        try
        {
            using var connection = GetConnection();
            var sql = @"
                SELECT c.CommentId, c.PostId, c.UserId, c.Content, c.IsHidden, c.CreatedAt, c.UpdatedAt,
                       u.UserName AS CommentUserName
                FROM Comments c
                INNER JOIN AspNetUsers u ON c.UserId = u.Id
                WHERE c.UserId = @UserId
                ORDER BY c.CreatedAt DESC";

            var comments = await connection.QueryAsync<Comment>(sql, new { UserId = userId });
            return comments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Comment> AddCommentAsync(Comment comment)
    {
        try
        {
            comment.CreatedAt = DateTime.UtcNow;
            comment.UpdatedAt = DateTime.UtcNow;

            using var connection = GetConnection();
            var sql = @"
                INSERT INTO Comments (PostId, UserId, Content, IsHidden, CreatedAt, UpdatedAt)
                VALUES (@PostId, @UserId, @Content, 0, @CreatedAt, @UpdatedAt);
                SELECT CAST(SCOPE_IDENTITY() as int);";

            comment.CommentId = await connection.ExecuteScalarAsync<int>(sql, comment);
            return comment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment for post {PostId} by user {UserId}", comment.PostId, comment.UserId);
            throw;
        }
    }



    public async Task<bool> DeleteCommentAsync(int commentId, string userId)
    {
        try
        {
            using var connection = GetConnection();
            var sql = "DELETE FROM Comments WHERE CommentId = @CommentId AND UserId = @UserId";
            var rows = await connection.ExecuteAsync(sql, new { CommentId = commentId, UserId = userId });
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment {CommentId} by user {UserId}", commentId, userId);
            throw;
        }
    }

    public async Task<bool> ToggleHideCommentAsync(int commentId)
    {
        try
        {
            using var connection = GetConnection();
            var current = await connection.QuerySingleOrDefaultAsync<bool?>(
                "SELECT IsHidden FROM Comments WHERE CommentId = @CommentId",
                new { CommentId = commentId }
            );

            if (current == null) return false;

            bool newValue = !current.Value;
            var sql = @"UPDATE Comments
                        SET IsHidden = @NewValue,
                            UpdatedAt = @UpdatedAt
                        WHERE CommentId = @CommentId";

            var rows = await connection.ExecuteAsync(sql, new { CommentId = commentId, NewValue = newValue, UpdatedAt = DateTime.UtcNow });
            return rows > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling hide for comment {CommentId}", commentId);
            throw;
        }
    }
}
