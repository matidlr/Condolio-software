using System.Text;
using Condolio.Api.Middleware;
using Condolio.Infrastructure;
using Condolio.Infrastructure.Auth;
using Condolio.Infrastructure.Identity;
using Condolio.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200"];

if (string.IsNullOrWhiteSpace(builder.Configuration["Storage:BasePath"]))
    builder.Configuration["Storage:BasePath"] =
        Path.Combine(builder.Environment.ContentRootPath, "App_Data", "archivos");

builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services
    .AddAuthentication(o =>
    {
        o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(o => o.AddPolicy("Frontend", p => p
    .WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "Condolio API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var sa = builder.Configuration.GetSection("SuperAdmin");
    await DataSeeder.SeedAsync(
        sp.GetRequiredService<CondolioDbContext>(),
        sp.GetRequiredService<RoleManager<IdentityRole>>(),
        sp.GetRequiredService<UserManager<ApplicationUser>>(),
        new SuperAdminSeed(sa["Email"] ?? "", sa["Password"] ?? "", sa["Nombre"] ?? "", sa["Apellido"] ?? ""));
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
