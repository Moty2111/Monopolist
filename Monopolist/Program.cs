using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;
using Monoplist.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Строка подключения (можно вынести в appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=DESKTOP-MFDQ1MT;Database=Monoplist;Integrated Security=true;MultipleActiveResultSets=true;Encrypt=False";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Настройка аутентификации: задаём схему по умолчанию и регистрируем две схемы
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "EmployeeCookie";  // схема по умолчанию для сотрудников
})
.AddCookie("EmployeeCookie", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "Monoplist.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
})
.AddCookie("CustomerCookie", options =>
{
    options.LoginPath = "/Account/CustomerLogin";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "Monoplist.Customer";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

builder.Services.AddAuthorization();

// Добавляем сервисы Razor Pages
builder.Services.AddRazorPages();

var app = builder.Build();

// Инициализация БД (только для разработки)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();
        SeedData.Initialize(context);
        SeedDataWarehouse.Initialize(context);
    }
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseMiddleware<SessionMiddleware>();
app.UseAuthorization();

app.MapRazorPages();

app.Run();