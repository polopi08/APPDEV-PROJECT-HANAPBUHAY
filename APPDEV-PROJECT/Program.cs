using Microsoft.EntityFrameworkCore;
using APPDEV_PROJECT.Data;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<HanapBuhayDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ClientSide"))
          .EnableSensitiveDataLogging()
          .LogTo(Console.WriteLine, LogLevel.Information)
          .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// ===== NEW: Add Authentication Service =====
// Configures cookie-based authentication for user login/logout
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/LoginPage";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/LoginPage";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

// ===== NEW: Add Session Service =====
// Stores temporary data (like NewUserId) during registration process
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ===== NEW: Add these middleware in correct order =====
// Session must be added before routing
app.UseSession();

// Authentication must be before authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=LandingPage}/{id?}")
    .WithStaticAssets();


app.Run();
