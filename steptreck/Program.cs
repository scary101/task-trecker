using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using QuestPDF.Infrastructure;
using steptreck.API.Gate;
using steptreck.API.Infrastructure.Email;
using steptreck.API.Infrastructure.File;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.API.Models;
using steptreck.API.Services.Auth;
using steptreck.API.Services.Backup;
using steptreck.API.Services.Chat;
using steptreck.API.Services.Dashboards;
using steptreck.API.Services.ImportExport;
using steptreck.API.Services.Members;
using steptreck.API.Services.Notifications;
using steptreck.API.Services.Owner;
using steptreck.API.Services.Projects;
using steptreck.API.Services.SessonWork;
using steptreck.API.Services.Subscriptions;
using steptreck.API.Services.Tasks;
using steptreck.API.Services.WorkUser;
using System.Security.Claims;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using steptreck.API.Services.Notes;
using steptreck.API.Services.Event;



var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

var minioConfig = builder.Configuration.GetSection("Minio");

builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var endpoint = minioConfig["Endpoint"];
    var accessKey = minioConfig["AccessKey"];
    var secretKey = minioConfig["SecretKey"];

    var useSslRaw = minioConfig["UseSSL"];
    var useSsl = false;
    if (!string.IsNullOrWhiteSpace(useSslRaw))
        bool.TryParse(useSslRaw, out useSsl);

    if (string.IsNullOrWhiteSpace(endpoint))
        throw new InvalidOperationException("Не задан Minio:Endpoint в appsettings.json");
    if (string.IsNullOrWhiteSpace(accessKey))
        throw new InvalidOperationException("Не задан Minio:AccessKey в appsettings.json");
    if (string.IsNullOrWhiteSpace(secretKey))
        throw new InvalidOperationException("Не задан Minio:SecretKey в appsettings.json");

    return new MinioClient()
        .WithEndpoint(endpoint)
        .WithCredentials(accessKey, secretKey)
        .WithSSL(useSsl)
        .Build();
});

var firebasePath = builder.Configuration["Firebase:ServiceAccountPath"];

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(firebasePath)
});

// CORS
const string CorsPolicyName = "WasmDev";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Token.ISSUER,

            ValidateAudience = true,
            ValidAudience = Token.AUDIENCE,

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = Token.GetSymmetricSecurityKey(),

            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        var frontendUrl = builder.Configuration["App:FrontendUrl"];
        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "https://localhost:7152",
            "http://localhost:7152",
            "https://localhost:7204",
            "http://localhost:7204",
            "https://localhost:8080",
            "http://localhost:8080",
            "https://127.0.0.1:7152",
            "http://127.0.0.1:7152",
            "https://127.0.0.1:7204",
            "http://127.0.0.1:7204",
            "https://127.0.0.1:8080",
            "http://127.0.0.1:8080"
        };

        if (!string.IsNullOrWhiteSpace(frontendUrl))
            origins.Add(frontendUrl);

        policy.WithOrigins(origins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var roleId = context.User.FindFirst("role_id")?.Value;
            if (roleId == ((int)steptreck.Domain.Enums.RoleEnum.Admin).ToString())
                return true;

            var roleName = context.User.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(roleName?.Trim(), "admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName?.Trim(), "админ", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(roleName?.Trim(), "администратор", StringComparison.OrdinalIgnoreCase);
        });
    });

    options.AddPolicy("OwnerOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context =>
        {
            var roleId = context.User.FindFirst("role_id")?.Value;
            if (roleId == ((int)steptreck.Domain.Enums.RoleEnum.Owner).ToString())
                return true;

            var roleName = context.User.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(roleName?.Trim(), "owner", StringComparison.OrdinalIgnoreCase);
        });
    });
});

builder.Services.AddOpenApi();
builder.Services.AddScoped<PushTokenService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RegisterService>();
builder.Services.AddScoped<TokenHelper>();
builder.Services.AddScoped<BlockService>();
builder.Services.AddTransient<EmailHelper>();
builder.Services.AddScoped<HeasherHelper>();
builder.Services.AddScoped<UserHelper>();
builder.Services.AddScoped<InvationTokenHelper>();
builder.Services.AddScoped<InviteServise>();
builder.Services.AddScoped<ProjectServise>();
builder.Services.AddScoped<FileManagerHelper>();
builder.Services.AddScoped<ProjectFileServise>();
builder.Services.AddScoped<MemberService>();
builder.Services.AddScoped<PlanServise>();
builder.Services.AddScoped<TeamFileServise>();
builder.Services.AddScoped<ProjectTeamService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<CheckListServise>();
builder.Services.AddScoped<AvatarService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ProjectTeamDashboardService>();
builder.Services.AddScoped<ImportExportDataService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<PrometheusClient>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ResetPasswordService>();
builder.Services.AddScoped<NotificationsService>();
builder.Services.AddScoped<NotificationsHelper>();
builder.Services.AddScoped<OwnerDashboardService>();
builder.Services.AddScoped<MemberScoreService>();
builder.Services.AddHostedService<DeadlineNotifierService>();
builder.Services.AddScoped<BackupService>();
builder.Services.AddScoped<TaskFileService>();
builder.Services.AddScoped<SubscriptionService>();
builder.Services.AddScoped<ReceiptPdfService>();
builder.Services.AddScoped<SubscriptionGuardMiddleware>();
builder.Services.AddScoped<ISubscriptionGate, SubscriptionGate>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<WorkSessionService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddHttpClient<TurnstileService>();
builder.Services.AddSignalR();


builder.Services.AddOpenTelemetry()
    .WithMetrics(m =>
    {
        m.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("steptreck.api"));
        m.AddAspNetCoreInstrumentation();
        m.AddRuntimeInstrumentation();
        m.AddPrometheusExporter();
    });
builder.Services.AddHttpClient<PrometheusClient>(http =>
{
    http.BaseAddress = new Uri(builder.Configuration["Prometheus:BaseUrl"] ?? "http://prometheus:9090");
    http.Timeout = TimeSpan.FromSeconds(10);
});


var app = builder.Build();

app.UseStaticFiles();


app.MapGet("/docs", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(Path.Combine(app.Environment.WebRootPath, "redoc.html"));
})
.AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}
app.UseRouting();
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<SubscriptionGuardMiddleware>();

app.MapControllers();
app.MapPrometheusScrapingEndpoint("/metrics");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<AuthHub>("/hubs/auth");
app.Run();
