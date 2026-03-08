using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Monoplist.Data;

var builder = WebApplication.CreateBuilder(args);

// Строка подключения (можно вынести в appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=DESKTOP-MFDQ1MT;Database=Monoplist;Integrated Security=true;MultipleActiveResultSets=true;Encrypt=False";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Настройка аутентификации с использованием кук
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization();

// Добавляем сервисы Razor Pages (без MVC)
builder.Services.AddRazorPages();

var app = builder.Build();

// Инициализация БД (только для разработки)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Создаём базу данных, если её нет (не удаляем существующую)
        context.Database.EnsureCreated();

        // Добавляем тестовые данные только если таблицы пусты
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
app.UseAuthorization();

// Маппинг Razor Pages
app.MapRazorPages();

app.Run();