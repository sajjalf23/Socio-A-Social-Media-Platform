using SocioApp.Models;
using SocioApp.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Dapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace SocioApp.Services
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<ProfileService> _logger;

        private readonly IConfiguration _configuration;
        public ProfileService(ApplicationDbContext context, Cloudinary cloudinary, ILogger<ProfileService> logger, IConfiguration configuration)
        {
            _context = context;
            _cloudinary = cloudinary;
            _logger = logger;
            _configuration = configuration;
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
        //     var connection = (SqlConnection)_context.Database.GetDbConnection();
        //     if (connection.State != System.Data.ConnectionState.Open)
        //         connection.Open();
        //     return connection;
        // }

        // private SqlConnection GetConnection()
        // {
        //     var connectionString = _configuration.GetConnectionString("DefaultConnection");
        //     if (string.IsNullOrWhiteSpace(connectionString))
        //         throw new InvalidOperationException("Database connection string is not configured.");

        //     return new SqlConnection(connectionString);
        // }

        private async Task<string?> UploadProfileImageAsync(IFormFile? imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.Length == 0) return null;

                using var stream = imageFile.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(imageFile.FileName, stream),
                    Folder = "socioapp/profiles",
                    UniqueFilename = true,
                    Overwrite = false
                };

                var result = await _cloudinary.UploadAsync(uploadParams);
                return result.StatusCode == System.Net.HttpStatusCode.OK ? result.SecureUrl.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile image.");
                throw;
            }
        }

        public async Task<ProfileViewModel?> GetProfileAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("GetProfileAsync called with empty userId");
                return null;
            }

            try
            {
                using var connection = GetConnection();
                userId = userId.Trim();

                var sql = @"
SELECT 
    [Id], [Name] AS Username, [Bio], [ProfilePicture] AS ProfileImage, [Email], [IsBanned],
    (SELECT COUNT(*) FROM Posts WHERE UserId = u.[Id]) AS PostsCount
FROM [AspNetUsers] u
WHERE [Id] = @UserId AND [IsBanned] = 0";

                var profile = await connection.QuerySingleOrDefaultAsync<ProfileViewModel>(
                    sql,
                    new { UserId = userId }
                );

                if (profile == null) return null;

                var postsSql = @"
SELECT PostId AS Id, MediaUrl
FROM Posts
WHERE UserId = @UserId AND IsHidden = 0
ORDER BY CreatedAt DESC";

                var postList = await connection.QueryAsync<PostViewModel>(postsSql, new { UserId = userId });
                profile.Posts = postList.ToList();

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ProfileViewModel?> GetProfilebyidFeed(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("GetProfilebyidAsync called with empty userId");
                return null;
            }

            try
            {
                using var connection = GetConnection();
                userId = userId.Trim();

                var sql = @"
            SELECT 
                [Id], 
                COALESCE([Name], [UserName], 'Unknown') AS Username, -- Fallback to UserName if Name is null
                [Bio], 
                COALESCE([ProfilePicture], '') AS ProfileImage, 
                [Email],
                ISNULL([IsBanned], 0) AS IsBanned,
                (SELECT COUNT(*) FROM Posts WHERE UserId = u.[Id]) AS PostsCount
            FROM [AspNetUsers] u
            WHERE [Id] = @UserId";

                var profile = await connection.QuerySingleOrDefaultAsync<ProfileViewModel>(
                    sql,
                    new { UserId = userId }
                );

                if (profile == null)
                {
                    _logger.LogWarning("Profile not found for user {UserId}", userId);
                    return null;
                }

                // Ensure ProfileImage has a default value
                if (string.IsNullOrWhiteSpace(profile.ProfileImage))
                {
                    profile.ProfileImage = "/images/defaultprofile.png";
                }

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for user {UserId}", userId);

                // Return a default profile instead of throwing
                return new ProfileViewModel
                {
                    Id = userId,
                    Username = "Unknown User",
                    ProfileImage = "/images/defaultprofile.png",
                    PostsCount = 0
                };
            }
        }
        public async Task<ProfileViewModel?> GetProfilebyidAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("GetProfileAsync called with empty userId");
                return null;
            }

            try
            {
                using var connection = GetConnection();
                userId = userId.Trim();

                var sql = @"
                         SELECT [Id], [Name] AS Username, [Bio], [ProfilePicture] AS ProfileImage, [Email], [IsBanned],
                          (SELECT COUNT(*) FROM Posts WHERE UserId = u.[Id]) AS PostsCount
                         FROM [AspNetUsers] u
                      WHERE [Id] = @UserId AND [IsBanned] = 0";

                var profile = await connection.QuerySingleOrDefaultAsync<ProfileViewModel>(
                    sql,
                    new { UserId = userId }
                );

                if (profile == null)
                {
                    _logger.LogWarning("Profile not found for user {UserId}", userId);
                    return null;
                }
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching profile for user {UserId}", userId);
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
        public async Task<bool> UpdateProfileAsync(string userId, string username, string name, string bio, IFormFile? profileImageFile, string preprofileimg)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("UpdateProfileAsync called with empty userId");
                return false;
            }

            try
            {
                using var connection = GetConnection();
                userId = userId.Trim();
                username = username?.Trim() ?? string.Empty;
                name = name?.Trim() ?? string.Empty;
                bio = bio?.Trim() ?? string.Empty;
                Console.WriteLine("Updating Profile for Username: " + username);
                string? profileImageUrl = await UploadProfileImageAsync(profileImageFile);
                Console.WriteLine("Uploaded Profile Image URL: " + profileImageUrl);
                var sql = @"
                    UPDATE AspNetUsers
                SET 
                UserName = @Username,
                Name = @Name,
                Bio = @Bio,
                ProfilePicture = CASE WHEN @ProfileImage IS NOT NULL THEN @ProfileImage ELSE ProfilePicture END,
                UpdatedAt = @UpdatedAt
                WHERE Id = @UserId";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    Username = username,
                    Name = name,
                    Bio = bio,
                    ProfileImage = profileImageUrl,
                    UpdatedAt = DateTime.UtcNow
                });

                if (rowsAffected == 1)
                {
                    var publicId = GetCloudinaryPublicId(preprofileimg);
                    if (!string.IsNullOrWhiteSpace(publicId))
                    {
                        var deletionResult = _cloudinary.Destroy(new DeletionParams(publicId));
                        Console.WriteLine("Deleted previous profile image: " + deletionResult.Result);
                    }
                }
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ToggleBanAsync(string userId)
        {
            try
            {
                using var connection = GetConnection();
                var current = await connection.QuerySingleOrDefaultAsync<bool?>(
                    "SELECT IsBanned FROM AspNetUsers WHERE Id = @UserId",
                    new { UserId = userId }
                );

                if (current == null) return false;

                bool newValue = !current.Value;

                var sql = @"
                    UPDATE AspNetUsers
                    SET IsBanned = @NewValue,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @UserId";

                var rows = await connection.ExecuteAsync(sql, new
                {
                    UserId = userId,
                    NewValue = newValue,
                    UpdatedAt = DateTime.UtcNow
                });

                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling ban for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<ProfileViewModel>> GetAllUsersAsync()
        {
            try
            {
                using var connection = GetConnection();
                var sql = @"
                    SELECT Id, UserName AS Username, Bio, ProfilePicture AS ProfileImage, Email, IsBanned,
                           (SELECT COUNT(*) FROM Posts WHERE UserId = u.Id) AS PostsCount
                    FROM AspNetUsers u
                    WHERE IsBanned = 0
                    ORDER BY CreatedAt DESC";

                var users = await connection.QueryAsync<ProfileViewModel>(sql);
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all active users.");
                throw;
            }
        }

        public async Task<IEnumerable<ProfileViewModel>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                using var connection = GetConnection();
                var sql = @"
                    SELECT Id, UserName AS Username, Bio, ProfilePicture AS ProfileImage, Email, IsBanned,
                           (SELECT COUNT(*) FROM Posts WHERE UserId = u.Id) AS PostsCount
                    FROM AspNetUsers u
                    WHERE IsBanned = 0 AND UserName LIKE @SearchTerm
                    ORDER BY UserName ASC";

                var users = await connection.QueryAsync<ProfileViewModel>(
                    sql, new { SearchTerm = $"%{searchTerm}%" });

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<ProfileViewModel>> SearchUsersForAdminAsync(string searchTerm)
        {
            try
            {
                using var connection = GetConnection();
                var sql = @"
                    SELECT Id, UserName AS Username, Bio, ProfilePicture AS ProfileImage, Email, IsBanned,
                           (SELECT COUNT(*) FROM Posts WHERE UserId = u.Id) AS PostsCount
                    FROM AspNetUsers u
                    WHERE UserName LIKE @SearchTerm
                    ORDER BY UserName ASC";

                var users = await connection.QueryAsync<ProfileViewModel>(
                    sql, new { SearchTerm = $"%{searchTerm}%" });

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users (admin) with term {SearchTerm}", searchTerm);
                throw;
            }
        }
    }
}
