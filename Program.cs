using Microsoft.EntityFrameworkCore;
using MobileAppBackend.Data;
using MobileAppBackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Entity Framework with PostgreSQL
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("DATABASE_URL environment variable is missing.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});


// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt_Issuer"],
            ValidAudience = builder.Configuration["Jwt_Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt_Key"]))
        };
    });

// Register services
builder.Services.AddScoped<UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


// Auto-create database and tables
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Принудительно создаем таблицы через SQL
        await dbContext.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" SERIAL PRIMARY KEY,
                ""Email"" TEXT NOT NULL,
                ""PasswordHash"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""CreatedAt"" TIMESTAMP NOT NULL DEFAULT NOW()
            );
            
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_Users_Email"" ON ""Users"" (""Email"");
        ");
        
        logger.LogInformation("✅ Таблицы созданы успешно!");
        
        // Проверяем
        var userCount = await dbContext.Users.CountAsync();
        logger.LogInformation("✅ Таблица Users готова, записей: {Count}", userCount);
        
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Ошибка при создании таблиц");
    }
}
// Тестовый эндпоинт для проверки БД
app.MapGet("/test-db", async (AppDbContext dbContext) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return Results.Ok(new { 
            database = "PostgreSQL", 
            connected = canConnect,
            tables = dbContext.Users.Any() ? "Users table exists" : "No tables found"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database error: {ex.Message}");
    }
});
app.Run();