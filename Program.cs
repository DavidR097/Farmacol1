using Farmacol.Filters;
using Farmacol.Middleware;
using Farmacol.Models;
using Farmacol.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<UserAreaFilter>();
});

builder.Services.AddDbContext<Farmacol1Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Conexion")));

builder.Services.AddScoped<NotificacionService>();
builder.Services.AddScoped<FlujoAprobacionService>();
builder.Services.AddScoped<UserAreaFilter>();
builder.Services.AddHostedService<SolicitudVencimientoService>();
builder.Services.AddScoped<PersonalRetiroService>();
builder.Services.AddScoped<DelegacionService>();
builder.Services.AddScoped<DocumentoService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ExcelService>();
builder.Services.AddScoped<VacacionesService>();
builder.Services.AddScoped<AnuncioService>();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 4;

    // Bloqueo tras 3 intentos fallidos — sin tiempo límite (solo admin puede desbloquear)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(365);
    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<Farmacol1Context>()
.AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SolicitudesAccess", policy =>
        policy.RequireRole("Administrador", "RRHH", "Directivo",
                           "Gerente", "Jefe", "Coordinador", "Asistente", "Usuario",
                           "Aprendiz", "Recepcionista", "TI"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login/Index";
    options.AccessDeniedPath = "/Login/Denegado";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseInhabilitadoCheck();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Login}/{action=Index}/{id?}");

// Seed de roles y admin
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // ── Crear roles ────────────────────────────────────────────────────
    string[] roles = {
        "Administrador", "Directivo", "Gerente", "RRHH", "Jefe",
        "Coordinador", "Asistente", "Usuario", "TI", "Aprendiz", "Recepcionista"
    };

    foreach (var rol in roles)
    {
        if (!await roleManager.RoleExistsAsync(rol))
            await roleManager.CreateAsync(new IdentityRole(rol));
    }

    // ── Crear usuario admin por defecto si no existe ───────────────────
    var adminUser = "admin";
    var adminPass = "Admin1234!";

    if (await userManager.FindByNameAsync(adminUser) == null)
    {
        var user = new IdentityUser { UserName = adminUser, Email = "admin@farmacol.com" };
        var result = await userManager.CreateAsync(user, adminPass);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "Administrador");
    }

    // Seed salas básicas si no existen
    var dbContext = scope.ServiceProvider.GetRequiredService<Farmacol1Context>();
    if (!await dbContext.TbSalas.AnyAsync())
    {
        dbContext.TbSalas.AddRange(
            new Farmacol.Models.TbSala { Nombre = "Sala Colombia", Activa = true },
            new Farmacol.Models.TbSala { Nombre = "Sala Vasoton", Activa = true },
            new Farmacol.Models.TbSala { Nombre = "Sala Apifolt", Activa = true }
        );
        await dbContext.SaveChangesAsync();
    }
}

app.Run();
