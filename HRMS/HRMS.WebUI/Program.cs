using HRMS.WebUI.Components;
using HRMS.Infrastructure;
using HRMS.Application;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using HRMS.WebUI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddApplicationServices();

// Cấu hình Cookie Authentication cho Blazor Server
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

// Đăng ký TempTokenStore dùng để chuyển tiếp Claims từ Interactive Server sang HTTP Endpoint ghi Cookie
builder.Services.AddSingleton<TempTokenStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Endpoint phụ trợ để thực hiện ghi cookie (Do SignalR không hỗ trợ ghi Header trực tiếp)
app.MapGet("/auth/signin", async (string token, TempTokenStore tokenStore, HttpContext httpContext) =>
{
    var principal = tokenStore.GetAndRemove(token);
    if (principal == null)
    {
        return Results.Redirect("/login?error=" + Uri.EscapeDataString("Yêu cầu đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng thử lại."));
    }

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new Microsoft.AspNetCore.Authentication.AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
        RedirectUri = "/"
    });

    return Results.Redirect("/");
});

// Endpoint thực hiện đăng xuất
app.MapGet("/auth/logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
