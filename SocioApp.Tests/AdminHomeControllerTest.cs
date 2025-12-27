using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SocioApp.Data;
using SocioApp.Models;
using SocioApp.Services;
using socio.Controllers;
using Xunit;

namespace SocioApp.Tests
{
    public class AdminHomeControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AdminHomeController _controller;
        private readonly Mock<IAdminService> _adminServiceMock;
        private readonly Mock<IProfileService> _profileServiceMock;
        private readonly Mock<ICommentService> _commentServiceMock;

        public AdminHomeControllerTests()
        {
            // Setup
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            _context = new ApplicationDbContext(options);
            SeedData();

            _adminServiceMock = new Mock<IAdminService>();
            _profileServiceMock = new Mock<IProfileService>();
            _commentServiceMock = new Mock<ICommentService>();

            SetupMocks();

            _controller = CreateController();
        }

        private void SeedData()
        {
            _context.Users.Add(new ApplicationUser 
            { 
                Id = "1", 
                UserName = "testuser", 
                Email = "test@example.com",
                IsBanned = false 
            });
            _context.SaveChanges();
        }

        private void SetupMocks()
        {
            // AdminService - FIXED: Added all required keys
            _adminServiceMock.Setup(x => x.GetUsersLast7DaysAsync())
                .ReturnsAsync(new List<int> { 1, 2, 3, 4, 5, 6, 7 });
            
            _adminServiceMock.Setup(x => x.GetPostsLast7DaysAsync())
                .ReturnsAsync(new List<int> { 1, 2, 3, 4, 5, 6, 7 });
            
            _adminServiceMock.Setup(x => x.GetCommentsLast7DaysAsync())
                .ReturnsAsync(new List<int> { 1, 2, 3, 4, 5, 6, 7 });
            
            _adminServiceMock.Setup(x => x.GetLikesLast7DaysAsync())
                .ReturnsAsync(new List<int> { 1, 2, 3, 4, 5, 6, 7 });
            
            _adminServiceMock.Setup(x => x.GetTotalCountsAsync())
                .ReturnsAsync(new Dictionary<string, int>
                {
                    { "TotalUsers", 100 },
                    { "TotalPosts", 500 },
                    { "TotalComments", 2000 },  // ADDED
                    { "TotalLikes", 10000 }     // ADDED
                });

            // ProfileService
            _profileServiceMock.Setup(x => x.GetAllUserforadmin())
                .ReturnsAsync(new List<ProfileViewModel>
                {
                    new ProfileViewModel { Id = "1", Username = "testuser" }
                });

            _profileServiceMock.Setup(x => x.SearchUsersAsync(It.IsAny<string>()))
                .ReturnsAsync(new List<ProfileViewModel>
                {
                    new ProfileViewModel { Id = "1", Username = "testuser" }
                });

            _profileServiceMock.Setup(x => x.ToggleBanAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            // CommentService
            _commentServiceMock.Setup(x => x.ToggleHideCommentAsync(It.IsAny<int>()))
                .ReturnsAsync(true);
        }

        private AdminHomeController CreateController()
        {
            var controller = new AdminHomeController(
                _adminServiceMock.Object,
                _profileServiceMock.Object,
                _commentServiceMock.Object);

            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("AdminPages", "true"),
                new Claim("CanBanUser", "true"),    // ADDED
                new Claim("CanHideComment", "true") // ADDED
            }));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            return controller;
        }

        // Simple Tests

        [Fact]
        public async Task Index_ShowsDashboard()
        {
            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["TotalUsers"]);
            Assert.NotNull(viewResult.ViewData["TotalPosts"]);
            Assert.NotNull(viewResult.ViewData["TotalComments"]); // ADDED
            Assert.NotNull(viewResult.ViewData["TotalLikes"]);    // ADDED
        }

        [Fact]
        public async Task AllUsers_ShowsUsers()
        {
            // Act
            var result = await _controller.AllUsers();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<ProfileViewModel>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task BanUser_Works()
        {
            // Act
            var result = await _controller.BanUnbanUser("1");

            // Assert - Now expecting JsonResult
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public async Task HideComment_Works()
        {
            // Act
            var result = await _controller.HideUnhideComment(1);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public async Task Search_WithText_FindsUsers()
        {
            // Act
            var result = await _controller.Search("test");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<ProfileViewModel>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Search_Empty_ReturnsEmpty()
        {
            // Act
            var result = await _controller.Search("");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<List<ProfileViewModel>>(viewResult.Model);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Comments_WithUserId_ReturnsView()
        {
            // Arrange
            _commentServiceMock.Setup(x => x.GetCommentsByUserAsync("1"))
                .ReturnsAsync(new List<Comment>
                {
                    new Comment { CommentId = 1, Content = "Test comment" }
                });

            // Act
            var result = await _controller.Comments("1");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }
    }
}