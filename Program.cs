using Microsoft.EntityFrameworkCore;
using KT_Learn.Data;
using DbUp;
using System.Reflection;
using Microsoft.OpenApi.Models;
using KT_Learn.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "������� ����� JWT � �������: Bearer {��� �����}"
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
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
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseNpgsql(connectionString)); // Connect DB context to real DataBase


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

builder.Services.AddScoped<TokenService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();
    await DbSeeder.SeedSuperAdminAsync(db, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication(); // initialize authentication
app.UseAuthorization();

app.MapControllers();

app.Run();
