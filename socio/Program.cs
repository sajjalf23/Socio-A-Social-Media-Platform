using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocioApp.Data;
using SocioApp.Models;
using CloudinaryDotNet;
using SocioApp.Services;
using Microsoft.AspNetCore.SignalR;
using SocioApp.Hubs;
using System.Security.Claims;
using SocioApp.Authorization;
using Microsoft.AspNetCore.Authorization;  

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000); 
    options.ListenAnyIP(5001, listenOptions => listenOptions.UseHttps()); 
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPages", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserPages", policy => policy.RequireRole("User", "Admin"));
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanHideComment", policy =>
        policy.RequireClaim("CanHideComment", "true"));

    options.AddPolicy("CanBanUser", policy =>
        policy.RequireClaim("CanBanUser", "true"));
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAuthorizationHandler, EditPostAuthorizationHandler>();

var cloudinarySettings = builder.Configuration.GetSection("Cloudinary");
var account = new Account(
    cloudinarySettings["CloudName"],
    cloudinarySettings["ApiKey"],
    cloudinarySettings["ApiSecret"]
);
builder.Services.AddSingleton(new Cloudinary(account));

builder.Services.AddSignalR(); 


var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

//  Map Routes 
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// Map SignalR Hub 
app.MapHub<NotificationHub>("/notificationHub");

app.MapGet("/Login", context =>
{
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

// Seed Roles & Admin 

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    foreach (var role in new[] { "Admin", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "admin@gmail.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            Name = "Super Admin",
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(adminUser, "Admin@123");
        if (!createResult.Succeeded)
            throw new Exception(string.Join(", ", createResult.Errors.Select(e => e.Description)));
    }

    if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        await userManager.AddToRoleAsync(adminUser, "Admin");

    
    var existingClaims = await userManager.GetClaimsAsync(adminUser);

    async Task EnsureClaim(string type, string value)
    {
        if (!existingClaims.Any(c => c.Type == type))
        {
            var result = await userManager.AddClaimAsync(adminUser, new Claim(type, value));
            if (!result.Succeeded)
                throw new Exception($"Failed to add claim {type}");
        }
    }

    await EnsureClaim("CanHideComment", "true");
    await EnsureClaim("CanBanUser", "true");
}


app.Run();
