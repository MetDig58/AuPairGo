using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext and MySQL Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AuPairGo.API.Data.ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Tell .NET to discover your Controllers folder
builder.Services.AddControllers();
var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "JWT signing key is missing. Configure Jwt:Key in user secrets or Azure App Service Settings."
    );
}

builder.Services
.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)),

        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],

        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],

        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero

    };
});

builder.Services.AddAuthorization();

// 3. Keep the OpenAPI generation tool active
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        var components = document.Components ??= new OpenApiComponents();

        components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();

        components.SecuritySchemes["Bearer"] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter your JWT token."
            };

        document.Security ??= new List<OpenApiSecurityRequirement>();

        document.Security.Add(
            new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecuritySchemeReference(
                        "Bearer",
                        document,
                        null)
                ] = new List<string>()
            });

        return Task.CompletedTask;
    });
});

var app = builder.Build();

// 4. Configure the HTTP request pipeline (Available in both Dev and Prod)
app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// 5. Explicitly map your Controller routes live
app.MapControllers();

app.Run();