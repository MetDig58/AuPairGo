using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Add DbContext and MySQL Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AuPairGo.API.Data.ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Tell .NET to discover your Controllers folder
builder.Services.AddControllers();

// 3. Keep the OpenAPI generation tool active
builder.Services.AddOpenApi();

var app = builder.Build();

// 4. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

// 5. Explicitly map your Controller routes live
app.MapControllers();

app.Run();