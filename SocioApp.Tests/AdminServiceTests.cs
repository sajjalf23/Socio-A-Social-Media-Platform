using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SocioApp.Data;
using SocioApp.Models;
using SocioApp.Services;
using Xunit;

public class AdminServiceTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly AdminService _adminService;

    public AdminServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()) 
            .Options;

        _dbContext = new ApplicationDbContext(options);

        _dbContext.Users.AddRange(new List<ApplicationUser>
        {
            new ApplicationUser { Id = "1", UserName = "user1", IsBanned = false },
            new ApplicationUser { Id = "2", UserName = "user2", IsBanned = false },
        });

        _dbContext.SaveChanges();

        var configurationMock = new Moq.Mock<IConfiguration>();

        _adminService = new AdminService(_dbContext, configurationMock.Object);
    }

    [Fact]
    public async Task GetUsersLast7DaysAsync_ReturnsCorrectCounts()
    {
        var result = await _adminService.GetUsersLast7DaysAsync();

        Assert.Equal(7, result.Count); 
        Assert.All(result, count => Assert.True(count >= 0)); 
    }
}
