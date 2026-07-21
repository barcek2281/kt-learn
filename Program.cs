using Microsoft.EntityFrameworkCore;
using KT_Learn.Data;
using DbUp;
using System.Reflection;
using Microsoft.OpenApi;
using KT_Learn.Exceptions;
using KT_Learn.Models;
using KT_Learn.Services;
using KT_Learn.Services.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Write down jwt token"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            new List<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)                             
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())    
    .LogToConsole()
    .Build();

var upgradeResult = upgrader.PerformUpgrade(); // Migration SQL
if (!upgradeResult.Successful)
{
    Console.WriteLine($"Migration falled off with error: {upgradeResult.Error}");
    return;   
}
// MapEnum обязателен в дополнение к HasPostgresEnum в AppDBContext: модель
// описывает тип для EF, а этот вызов учит сам драйвер Npgsql передавать
// значение как user_role, а не как integer.
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MapEnum<Role>("user_role"))); // Connect DB context to real DataBase


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear(); 

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,            
            ValidateIssuerSigningKey = true,   
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            RoleClaimType = "role"   
        };
    });

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
    await DbSeeder.SeedSuperAdminAsync(db, app.Configuration);
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler(); // должен идти первым, чтобы ловить всё, что ниже

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // initialize authentication
app.UseAuthorization();

app.MapControllers();

app.Run();
