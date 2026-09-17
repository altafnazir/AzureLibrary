var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapGet("/debug/headers", (HttpRequest request) =>
{
    using var client = new HttpClient();

    var ip = client.GetStringAsync("https://api.ipify.org").Result;

    return Results.Ok(new
    {
        Host = request.Host.ToString(),
        Scheme = request.Scheme,
        Path = request.Path.ToString(),
        IP = ip
    });
});

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
