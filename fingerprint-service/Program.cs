using fingerprint_service.db;
using fingerprint_service.Repository;
using fingerprint_service.Repository.Interfaces;
using fingerprint_service.Services;
using fingerprint_service.Services.Interfaces;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<IFingerprintService, FingerprintService>();
builder.Services.AddScoped<IFingerprintRepository, FingerprintRepository>();

// Registra DbConnection como un servicio
builder.Services.AddScoped<DbConnection>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();