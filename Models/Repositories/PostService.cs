using Dapper;
using Microsoft.Data.SqlClient;
using SocioApp.Data;
using SocioApp.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SocioApp.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<PostService> _logger;

        private readonly IConfiguration _configuration;

        private readonly string _connectionString;

        public PostService(ApplicationDbContext context, Cloudinary cloudinary, ILogger<PostService> logger, IConfiguration configuration)
        {
            _context = context;
            _cloudinary = cloudinary;
            _logger = logger;
            _configuration = configuration; // assign it
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException(
                     "DefaultConnection is missing from configuration.");
        }

        private SqlConnection GetConnection()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Database connection string is not configured.");

            return new SqlConnection(connectionString);
        }

        // private SqlConnection GetConnection()
        // {
        //     return new SqlConnection(_connectionString);
        // }


        public async Task<Post?> GetPostByIdAsync(int postId)
        {
            try
            {
                using var connection = GetConnection();

                const string sql = @"
            SELECT PostId, UserId, Content, MediaUrl, IsHidden, LikesCount, 
            DislikesCount, CreatedAt, UpdatedAt
            FROM Posts
            WHERE PostId = @PostId";

                var post = await connection.QuerySingleOrDefaultAsync<Post>(sql, new { PostId = postId });

                return post == null || post.IsHidden ? null : post;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching post {PostId}", postId);
                throw;
            }
        }


        private string GetCloudinaryPublicId(string mediaUrl)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mediaUrl))
                {
                    _logger.LogWarning("GetCloudinaryPublicId called with empty mediaUrl");
                    return "";
                }

                var uri = new Uri(mediaUrl);
                var path = uri.AbsolutePath;

                var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
                int uploadIndex = Array.IndexOf(parts, "upload");

                if (uploadIndex == -1 || uploadIndex + 1 >= parts.Length)
                    return "";
                string publicId = string.Join("/", parts[(uploadIndex + 1)..]);
                publicId = Path.ChangeExtension(publicId, null);

                return publicId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting Cloudinary public ID from {MediaUrl}", mediaUrl);
                return "";
            }
        }

        public async Task<int> CreatePostAsync(string userId, string caption, IFormFile? imageFile)
        {
            try
            {
                string? mediaUrl = null;

                if (imageFile != null && imageFile.Length > 0)
                {
                    using var stream = imageFile.OpenReadStream();
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(imageFile.FileName, stream),
                        Folder = "socioapp/posts",
                        UniqueFilename = true,
                        Overwrite = false
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    if (uploadResult?.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new Exception("Failed to upload image to Cloudinary.");

                    mediaUrl = uploadResult.SecureUrl.ToString();
                }

                using var connection = GetConnection();
                var sql = @"
                    INSERT INTO Posts 
                        (UserId, Content, MediaUrl, IsHidden, LikesCount, DislikesCount, CreatedAt, UpdatedAt)
                    VALUES 
                        (@UserId, @Content, @MediaUrl, 0, 0, 0, @CreatedAt, @UpdatedAt);
                    SELECT CAST(SCOPE_IDENTITY() as int);";

                var postId = await connection.ExecuteScalarAsync<int>(sql, new
                {
                    UserId = userId,
                    Content = caption,
                    MediaUrl = mediaUrl,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                return postId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> EditPostAsync(int postId, string userId, string caption, IFormFile? imageFile)
        {
            try
            {
                string? newMediaUrl = null;

                if (imageFile != null && imageFile.Length > 0)
                {
                    using var stream = imageFile.OpenReadStream();
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(imageFile.FileName, stream),
                        Folder = "socioapp/posts",
                        UniqueFilename = true,
                        Overwrite = false
                    };

                    var uploadResult = await _cloudinary.UploadAsync(uploadParams);
                    if (uploadResult.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new Exception("Image upload failed.");

                    newMediaUrl = uploadResult.SecureUrl.ToString();
                }

                using var connection = GetConnection();
                var sql = @"
                    UPDATE Posts
                    SET Content = @Content,
                        MediaUrl = CASE WHEN @MediaUrl IS NOT NULL THEN @MediaUrl ELSE MediaUrl END,
                        UpdatedAt = @UpdatedAt
                    WHERE PostId = @PostId AND UserId = @UserId";

                var rows = await connection.ExecuteAsync(sql, new
                {
                    PostId = postId,
                    UserId = userId,
                    Content = caption,
                    MediaUrl = newMediaUrl,
                    UpdatedAt = DateTime.UtcNow
                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing post {PostId} for user {UserId}", postId, userId);
                throw;
            }
        }
        public async Task<bool> DeletePostAsync(int postId, string userId)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                var post = await connection.QuerySingleOrDefaultAsync<Post>(
                    "SELECT PostId, UserId, MediaUrl FROM Posts WHERE PostId = @PostId AND UserId = @UserId",
                    new { PostId = postId, UserId = userId },
                    transaction
                );

                if (post == null)
                {
                    transaction.Rollback();
                    return false;
                }
                if (!string.IsNullOrEmpty(post.MediaUrl))
                {
                    string publicId = GetCloudinaryPublicId(post.MediaUrl);
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                    }
                }
                await connection.ExecuteAsync(
                    "DELETE FROM PostReactions WHERE PostId = @PostId",
                    new { PostId = postId },
                    transaction
                );
                await connection.ExecuteAsync(
                    "DELETE FROM Comments WHERE PostId = @PostId",
                    new { PostId = postId },
                    transaction
                );
                var rows = await connection.ExecuteAsync(
                    "DELETE FROM Posts WHERE PostId = @PostId AND UserId = @UserId",
                    new { PostId = postId, UserId = userId },
                    transaction
                );

                transaction.Commit();
                return rows > 0;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error deleting post {PostId}", postId);
                throw;
            }
        }




        public async Task<bool> ToggleHidePostAsync(int postId)
        {
            try
            {
                using var connection = GetConnection();
                var current = await connection.QuerySingleOrDefaultAsync<bool?>(
                    "SELECT IsHidden FROM Posts WHERE PostId = @PostId",
                    new { PostId = postId }
                );

                if (current == null) return false;

                bool newValue = !current.Value;
                var sql = @"
                    UPDATE Posts
                    SET IsHidden = @NewValue,
                        UpdatedAt = @UpdatedAt
                    WHERE PostId = @PostId";

                var rows = await connection.ExecuteAsync(sql, new { PostId = postId, NewValue = newValue, UpdatedAt = DateTime.UtcNow });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling hide post {PostId}", postId);
                throw;
            }
        }

        public async Task<IEnumerable<dynamic>> GetAllPostsAsync(bool includeHidden = false)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();

                var sql = "SELECT PostId FROM Posts";

                if (!includeHidden)
                    sql += " WHERE IsHidden = 0";

                sql += " ORDER BY CreatedAt DESC";

                var posts = await connection.QueryAsync<dynamic>(sql);
                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all posts (includeHidden={IncludeHidden})", includeHidden);
                throw;
            }
        }
        public async Task<bool> LikePostAsync(int postId, string userId)
        {
            using var connection = GetConnection();
            await connection.OpenAsync(); // Must open connection before transaction
            using var transaction = connection.BeginTransaction();

            try
            {
                var sqlRemoveDislike = @"
                DELETE FROM PostReactions
                WHERE PostId = @PostId AND UserId = @UserId AND Type = 'Dislike'";
                await connection.ExecuteAsync(sqlRemoveDislike, new { PostId = postId, UserId = userId }, transaction);

                var sqlCheckLike = @"
                SELECT COUNT(*) FROM PostReactions
                WHERE PostId = @PostId AND UserId = @UserId AND Type = 'Like'";
                var alreadyLiked = await connection.ExecuteScalarAsync<int>(sqlCheckLike, new { PostId = postId, UserId = userId }, transaction);

                if (alreadyLiked > 0)
                {
                    // Remove like (toggle)
                    var sqlRemoveLike = @"
                    DELETE FROM PostReactions
                    WHERE PostId = @PostId AND UserId = @UserId AND Type = 'Like'";
                    await connection.ExecuteAsync(sqlRemoveLike, new { PostId = postId, UserId = userId }, transaction);
                }
                else
                {
                    // Add like
                    // var sqlAddLike = @"
                    // INSERT INTO PostReactions (PostId, UserId, Type)
                    // VALUES (@PostId, @UserId, 'Like')";
                    // await connection.ExecuteAsync(sqlAddLike, new { PostId = postId, UserId = userId }, transaction);
                    var sqlAddLike = @"
                      INSERT INTO PostReactions (PostId, UserId, Type, CreatedAt)
                          VALUES (@PostId, @UserId, 'Like', @CreatedAt)";

                    await connection.ExecuteAsync(sqlAddLike, new
                    {
                        PostId = postId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    }, transaction);

                }

                // Update counts in Posts table
                var sqlUpdateCounts = @"
                UPDATE Posts
                SET LikesCount = (SELECT COUNT(*) FROM PostReactions WHERE PostId = @PostId AND Type = 'Like'),
                    DislikesCount = (SELECT COUNT(*) FROM PostReactions WHERE PostId = @PostId AND Type = 'Dislike'),
                    UpdatedAt = @UpdatedAt
                WHERE PostId = @PostId";
                await connection.ExecuteAsync(sqlUpdateCounts, new { PostId = postId, UpdatedAt = DateTime.UtcNow }, transaction);

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error liking post {PostId} by user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<bool> DislikePostAsync(int postId, string userId)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var sqlRemoveLike = @"
                DELETE FROM PostReactions
                WHERE PostId = @PostId AND UserId = @UserId AND Type = 'Like'";
                await connection.ExecuteAsync(sqlRemoveLike, new { PostId = postId, UserId = userId }, transaction);

                var sqlCheckDislike = @"
                SELECT COUNT(*) FROM PostReactions
                WHERE PostId = @PostId AND UserId = @UserId AND Type = 'Dislike'";
                var alreadyDisliked = await connection.ExecuteScalarAsync<int>(sqlCheckDislike, new { PostId = postId, UserId = userId }, transaction);

                if (alreadyDisliked > 0)
                {
                    var sqlRemoveDislike = @"
                    DELETE FROM PostReactions
                    WHERE PostId = @PostId AND UserId = @UserId AND Type = 'Dislike'";
                    await connection.ExecuteAsync(sqlRemoveDislike, new { PostId = postId, UserId = userId }, transaction);
                }
                else
                {
                    //     var sqlAddDislike = @"
                    //     INSERT INTO PostReactions (PostId, UserId, Type)
                    //     VALUES (@PostId, @UserId, 'Dislike')";
                    //     await connection.ExecuteAsync(sqlAddDislike, new { PostId = postId, UserId = userId }, transaction);
                    // 
                    var sqlAddDislike = @"
INSERT INTO PostReactions (PostId, UserId, Type, CreatedAt)
VALUES (@PostId, @UserId, 'Dislike', @CreatedAt)";

                    await connection.ExecuteAsync(sqlAddDislike, new
                    {
                        PostId = postId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    }, transaction);

                }

                var sqlUpdateCounts = @"
                UPDATE Posts
                SET LikesCount = (SELECT COUNT(*) FROM PostReactions WHERE PostId = @PostId AND Type = 'Like'),
                    DislikesCount = (SELECT COUNT(*) FROM PostReactions WHERE PostId = @PostId AND Type = 'Dislike'),
                    UpdatedAt = @UpdatedAt
                WHERE PostId = @PostId";
                await connection.ExecuteAsync(sqlUpdateCounts, new { PostId = postId, UpdatedAt = DateTime.UtcNow }, transaction);

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError(ex, "Error disliking post {PostId} by user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<string?> GetUserReactionAsync(int postId, string userId)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            try
            {
                var sql = @"
                SELECT Type 
                FROM PostReactions
                WHERE PostId = @PostId AND UserId = @UserId";

                var reaction = await connection.QuerySingleOrDefaultAsync<string>(sql, new { PostId = postId, UserId = userId });
                return reaction;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reaction for post {PostId} by user {UserId}", postId, userId);
                throw;
            }
        }



    }
}
