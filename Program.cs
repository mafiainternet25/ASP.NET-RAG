using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using web.Data;
using web.Models;
using web.Security;
using web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .SetBasePath(Path.Combine(builder.Environment.ContentRootPath, "config"))
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("QuocPhim");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options
        .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
        .UseSnakeCaseNamingConvention());

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<AuthSessionService>();
builder.Services.AddScoped<CurrentUserResolver>();
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<ShowtimeService>();
builder.Services.AddScoped<ChatbotService>();
builder.Services.AddHttpClient();
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai:Groq"));

builder.Services
    .AddAuthentication(TokenAuthenticationDefaults.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>(
        TokenAuthenticationDefaults.AuthenticationScheme,
        _ =>
        {
        });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(TokenAuthenticationDefaults.AdminOnlyPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(TokenAuthenticationDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("ADMIN");
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var viewsPath = Path.Combine(app.Environment.ContentRootPath, "Views");
if (Directory.Exists(viewsPath))
{
    var provider = new PhysicalFileProvider(viewsPath);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = provider,
        DefaultFileNames = { "index.html" }
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = provider,
        RequestPath = ""
    });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapFallback(async context =>
{
    var indexPath = Path.Combine(app.Environment.ContentRootPath, "Views", "index.html");
    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(indexPath);
        return;
    }

    context.Response.StatusCode = StatusCodes.Status404NotFound;
});

app.Run();
