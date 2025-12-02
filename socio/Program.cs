using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocioApp.Data;
using SocioApp.Models;
using CloudinaryDotNet;
using SocioApp.Services;

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

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IProfileService, ProfileService>();


var cloudinarySettings = builder.Configuration.GetSection("Cloudinary");
var account = new Account(
    cloudinarySettings["CloudName"],
    cloudinarySettings["ApiKey"],
    cloudinarySettings["ApiSecret"]
);

builder.Services.AddSingleton(new Cloudinary(account));

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.MapGet("/Login", context =>
{
    context.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = new[] { "Admin", "User" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    var adminEmail = "admin@socio.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            Name = "Super Admin"
        };
        await userManager.CreateAsync(adminUser, "Admin@123");
        await userManager.AddToRoleAsync(adminUser, "Admin");

        await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("CanEditPosts", "true"));
        await userManager.AddClaimAsync(adminUser, new System.Security.Claims.Claim("CanDeletePosts", "true"));
    }
}

app.Run();
