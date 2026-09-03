var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapGet("/health/live", () =>
{
    return Results.Ok("Alive");
    //return Results.StatusCode(503);
});

app.MapGet("/health/ready", () =>
{
    return Results.Ok("Ready");
    //return Results.StatusCode(503);
});

app.MapGet("/api/hello", () =>
{
    var instanceName = Environment.GetEnvironmentVariable("INSTANCE_NAME") ?? "local";
    
    return Results.Ok(new { message = $"Hello from {instanceName}!", instance = instanceName, machinename = Environment.MachineName, timestamputc = DateTime.UtcNow });
});

app.MapGet("/api/cpu-test", () =>
{
    var end = DateTime.UtcNow.AddMilliseconds(500);

    while (DateTime.UtcNow < end)
    {
        Math.Sqrt(Random.Shared.NextDouble());
    }

    return Results.Ok("CPU work completed");
});


// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.MapOpenApi();
// }

// app.UseHttpsRedirection();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// app.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
