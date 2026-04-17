using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using web.Data;
using web.Models;
using web.Security;
using web.Services;
using System.Diagnostics;

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
builder.Services.AddScoped<JwtUtil>();
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
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pages}/{action=Index}/{id?}");

app.MapFallbackToController("Index", "Pages");

if (app.Environment.IsDevelopment())
{
    try
    {
        var ragServicePath = Path.Combine(app.Environment.ContentRootPath, "RagService", "rag_service.py");
        var ragServiceDir = Path.Combine(app.Environment.ContentRootPath, "RagService");
        if (File.Exists(ragServicePath))
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python3",
                    Arguments = "rag_service.py",
                    WorkingDirectory = ragServiceDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    Console.WriteLine($"[RAG Service] {args.Data}");
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    Console.WriteLine($"[RAG Service ERROR] {args.Data}");
            };

            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) =>
            {
                Console.WriteLine($"⚠ RAG Service exited with code {process.ExitCode}");
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Console.WriteLine("✓ RAG Service started");

            app.Lifetime.ApplicationStopping.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Error stopping RAG Service: {ex.Message}");
                }
            });
        }
        else
        {
            Console.WriteLine($"⚠ RAG Service not found at {ragServicePath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Failed to start RAG Service: {ex.Message}");
        Console.WriteLine($"  Stack trace: {ex.StackTrace}");
    }
}

app.Run();
