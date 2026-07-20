using HRMS.Application;
using HRMS.Application.Interfaces;
using HRMS.Application.Services;
using HRMS.Infrastructure;
using HRMS.Infrastructure.Persistence;
using HRMS.Infrastructure.Services;
using HRMS.WebUI.Components;
using HRMS.WebUI.Hubs;
using HRMS.WebUI.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// Add services to the container.
// ----------------------------------------------------------------------------
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddRazorPages();

// Cookie Authentication cho Blazor Server
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "HRMS_AuthCookie";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// TempTokenStore: chuyển tiếp Claims từ Interactive Server sang HTTP Endpoint để ghi Cookie
builder.Services.AddSingleton<TempTokenStore>();

// ----------------------------------------------------------------------------
// Module Request Workflow (Task 3.2)
// DB / DbConcurrencyGate / IEmployeeLookup đã được đăng ký sẵn bên trong
// AddInfrastructureServices() ở trên - chỉ còn thiếu 2 dòng dưới đây.
// ----------------------------------------------------------------------------
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IRequestNotifier, SignalRRequestNotifier>();

builder.Services.AddSignalR();

var app = builder.Build();

// ----------------------------------------------------------------------------
// Configure the HTTP request pipeline.
// ----------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// UseAntiforgery() phải đặt SAU UseAuthentication/UseAuthorization theo khuyến nghị của Microsoft
app.UseAntiforgery();

// Endpoint phụ trợ để ghi Cookie (SignalR/Blazor Server không hỗ trợ ghi Header trực tiếp)
app.MapGet("/auth/signin", async (string token, TempTokenStore tokenStore, HttpContext httpContext) =>
{
    var principal = tokenStore.GetAndRemove(token);
    if (principal == null)
    {
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("Yêu cầu đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng thử lại."));
    }

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
        RedirectUri = "/"
    });

    return Results.Redirect("/");
});

app.MapGet("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapHub<RequestHub>("/hubs/requests");

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();