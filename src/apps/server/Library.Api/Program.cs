using Library.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddLibraryInfrastructure(builder.Configuration);
builder.Services.AddCors(options =>
{
    options.AddPolicy("client", policy =>
    {
        var allowedOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:3000";
        policy.WithOrigins(allowedOrigin).AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

await app.Services.InitializeLibraryDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("client");
app.MapControllers();

app.Run();

public partial class Program;
