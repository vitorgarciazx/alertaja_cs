using AlertaJa.Api.Data;
using AlertaJa.Api.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("OracleConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(connectionString));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter()
        );
    });

builder.Services.AddOpenApi();

builder.Services.AddScoped<RegiaoService>();
builder.Services.AddScoped<SensorService>();
builder.Services.AddScoped<MedicaoService>();
builder.Services.AddScoped<AlertaService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();