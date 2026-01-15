using Microsoft.EntityFrameworkCore;
using APPDEV_PROJECT.Data;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<HanapBuhayDBContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("ClientSide")));

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

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=LandingPage}/{id?}")
    .WithStaticAssets();


app.Run();
