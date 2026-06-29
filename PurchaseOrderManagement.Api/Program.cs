using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using PurchaseOrderManagement.Api.Data;
using PurchaseOrderManagement.Api.Infrastructure;
using PurchaseOrderManagement.Api.Seeding;
using PurchaseOrderManagement.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Admin slice (Companies, Users, Roles) — docs/01, docs/08.
builder.Services.AddScoped<IAdminAuthorizer, AdminAuthorizer>();
builder.Services.AddScoped<IRoleHierarchyService, RoleHierarchyService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();

// Procurement slice (Suppliers, Files, Quotations, SupplierBids) — docs/02, docs/07.
// Stateless disk/URL helpers are Singleton; everything touching the DbContext is Scoped.
builder.Services.AddSingleton<IFileStorage, LocalDiskFileStorage>();
builder.Services.AddSingleton<IFileUrlResolver, FileUrlResolver>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IQuotationService, QuotationService>();
builder.Services.AddScoped<IBidService, BidService>();

// Cookie-based server session auth (docs/05-CROSS-CUTTING-CONVENTIONS.md): httpOnly, Secure,
// SameSite=Strict, sliding expiration. This is an API consumed by a SPA, not a page-rendering
// app, so unauthenticated/forbidden requests must return 401/403 status codes rather than
// redirecting to a login page (the default cookie-auth behavior).
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "pom_session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

// Translate ServiceException thrown by services into consistent ProblemDetails responses.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ServiceExceptionFilter>();
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DataSeeder.SeedAsync(app.Services);

app.Run();
