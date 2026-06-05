using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using steptreck.Web;
using steptreck.Web.Services;
using steptreck.Web.ViewModel;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IInviteTokenStore, InviteTokenStore>();
builder.Services.AddScoped<IUserRoleStore, UserRoleStore>();
builder.Services.AddScoped<IRoleAccessService, RoleAccessService>();
builder.Services.AddScoped<ISubscriptionCheckState, SubscriptionCheckState>();

builder.Services.AddScoped<JwtAuthHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<JwtAuthHandler>();
    handler.InnerHandler = new HttpClientHandler();

    var apiBaseUrl = builder.Configuration["Api:BaseUrl"];
    if (string.IsNullOrWhiteSpace(apiBaseUrl))
        apiBaseUrl = builder.HostEnvironment.BaseAddress;

    return new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseUrl)
    };
});


builder.Services.AddScoped<PlanVM>();
builder.Services.AddScoped<AuthViewModel>();
builder.Services.AddScoped<InviteViewModel>();
builder.Services.AddScoped<MembersVm>();
builder.Services.AddScoped<ProjectFilesViewModel>();
builder.Services.AddScoped<ProjectsViewModel>();
builder.Services.AddScoped<ProjectTeamsViewModel>();
builder.Services.AddScoped<RegisterViewModel>();
builder.Services.AddScoped<TaskViewModel>();
builder.Services.AddScoped<TaskCheckListViewModel>();
builder.Services.AddScoped<TeamFilesViewModel>();
builder.Services.AddScoped<TaskFilesViewModel>();
builder.Services.AddScoped<ChatViewModel>();
builder.Services.AddScoped<ChatClient>();
builder.Services.AddScoped<OpsMetricsVm>();
builder.Services.AddScoped<AuditViewModel>();
builder.Services.AddScoped<ResetPasswordViewModel>();
builder.Services.AddScoped<AuditViewModel>();
builder.Services.AddScoped<NotificationsViewModel>();
builder.Services.AddScoped<MemberScoreViewModel>();
builder.Services.AddScoped<BackupViewModel>();
builder.Services.AddScoped<SubscriptionsViewModel>();
builder.Services.AddScoped<OwnerDashboardViewModel>();
builder.Services.AddScoped<ProjectTeamDashboardViewModel>();
builder.Services.AddScoped<WorkSessionViewModel>();
builder.Services.AddScoped<CalendarViewModel>();

await builder.Build().RunAsync();
