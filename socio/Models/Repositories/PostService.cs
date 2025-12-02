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

        public PostService(ApplicationDbContext context, Cloudinary cloudinary, ILogger<PostService> logger, IConfiguration configuration)
        {
            _context = context;
            _cloudinary = cloudinary;
            _logger = logger;
            _configuration = configuration; // assign it
        }

        private SqlConnection GetConnection()
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Database connection string is not configured.");

            return new SqlConnection(connectionString);
        }

        public async Task<Post?> GetPostByIdAsync(int postId)
        {
            try
            {
                using var connection = GetConnection();
                await connection.OpenAsync();

                const string sql = @"
                SELECT PostId, UserId, Content, MediaUrl, IsHidden, LikesCount, DislikesCount, CreatedAt, UpdatedAt
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
                var uri = new Uri(mediaUrl);
                var path = uri.AbsolutePath;
                var parts = path.Split('/');
                int uploadIndex = Array.IndexOf(parts, "upload");
                if (uploadIndex == -1 || uploadIndex + 2 >= parts.Length) return "";
                string publicId = string.Join("/", parts[(uploadIndex + 2)..]);
                publicId = System.IO.Path.ChangeExtension(publicId, null);
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
        public async Task<bool> LikeAsync(int postId)
        {
            try
            {
                using var connection = GetConnection();
                var sql = @"
            UPDATE Posts
            SET LikesCount = LikesCount + 1,
                UpdatedAt = @UpdatedAt
            WHERE PostId = @PostId";

                var rows = await connection.ExecuteAsync(sql, new { PostId = postId, UpdatedAt = DateTime.UtcNow });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking post {PostId}", postId);
                return false;
            }
        }

        public async Task<bool> DislikeAsync(int postId)
        {
            try
            {
                using var connection = GetConnection();
                var sql = @"
            UPDATE Posts
            SET LikesCount = CASE WHEN LikesCount > 0 THEN LikesCount - 1 ELSE 0 END,
                UpdatedAt = @UpdatedAt
            WHERE PostId = @PostId";

                var rows = await connection.ExecuteAsync(sql, new { PostId = postId, UpdatedAt = DateTime.UtcNow });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disliking post {PostId}", postId);
                return false;
            }
        }


        public async Task<bool> DeletePostAsync(int postId, string userId)
        {
            try
            {
                using var connection = GetConnection();
                var post = await connection.QuerySingleOrDefaultAsync<Post>(
                    "SELECT PostId, UserId, MediaUrl FROM Posts WHERE PostId = @PostId AND UserId = @UserId",
                    new { PostId = postId, UserId = userId }
                );

                if (post == null) return false;

                if (!string.IsNullOrEmpty(post.MediaUrl))
                {
                    string publicId = GetCloudinaryPublicId(post.MediaUrl);
                    if (!string.IsNullOrEmpty(publicId))
                    {
                        await _cloudinary.DestroyAsync(new DeletionParams(publicId));
                    }
                }

                var sql = "DELETE FROM Posts WHERE PostId = @PostId AND UserId = @UserId";
                var rows = await connection.ExecuteAsync(sql, new { PostId = postId, UserId = userId });

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId} for user {UserId}", postId, userId);
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

        public async Task<IEnumerable<Post>> GetAllPostsAsync(bool includeHidden = false)
        {
            try
            {
                using var connection = GetConnection();
                string sql = @"
                    SELECT PostId, UserId, Content, MediaUrl, IsHidden, LikesCount, DislikesCount, CreatedAt, UpdatedAt
                    FROM Posts";

                if (!includeHidden) sql += " WHERE IsHidden = 0";
                sql += " ORDER BY CreatedAt DESC";

                var posts = await connection.QueryAsync<Post>(sql);
                return posts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all posts (includeHidden={IncludeHidden})", includeHidden);
                throw;
            }
        }

        public async Task<bool> LikePostAsync(int postId)
        {
            try
            {
                using var connection = GetConnection();
                var sql = @"
                    UPDATE Posts
                    SET LikesCount = LikesCount + 1,
                        UpdatedAt = @UpdatedAt
                    WHERE PostId = @PostId";

                var rows = await connection.ExecuteAsync(sql, new { PostId = postId, UpdatedAt = DateTime.UtcNow });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking post {PostId}", postId);
                throw;
            }
        }

        public async Task<bool> DislikePostAsync(int postId) 
        {
            try
            {
                using var connection = GetConnection();
                var sql = @"
                    UPDATE Posts
                    SET DislikesCount = DislikesCount + 1,
                        UpdatedAt = @UpdatedAt
                    WHERE PostId = @PostId";

                var rows = await connection.ExecuteAsync(sql, new { PostId = postId, UpdatedAt = DateTime.UtcNow });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disliking post {PostId}", postId);
                throw;
            }
        }
    }
}
