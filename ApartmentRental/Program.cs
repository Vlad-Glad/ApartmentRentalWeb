using ApartmentRental.Data;
using ApartmentRental.Models;
using ApartmentRental.Search;
using ApartmentRental.Services;
using ApartmentRental.Services.ExchangeRates;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Map extra profile fields
        options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
        options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
        options.ClaimActions.MapJsonKey("urn:google:picture", "picture");

        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Identity/Account/Login?error=external_canceled");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

// Typed HttpClient for PrivatBank service
builder.Services.AddHttpClient<IExchangeRateService, PrivatBankExchangeRateService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

builder.Services.AddScoped<IBlobService, BlobService>();

builder.Services.Configure<AzureSearchOptions>(builder.Configuration.GetSection("AzureSearch"));
builder.Services.AddSingleton<IApartmentSearchService, ApartmentSearchService>();

builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (!app.Environment.IsEnvironment("Docker"))
{
    app.UseHttpsRedirection();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Apartments}/{action=Index}/{id?}");

app.MapRazorPages();
app.Run();
