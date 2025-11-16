var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHttpClient("ProductsService", client => 
{
    client.BaseAddress = new Uri(builder.Configuration["ProductsService:BaseUrl"] ?? "http://localhost:3001");
});
builder.Services.AddHttpClient("InventoryService", client => 
{
    client.BaseAddress = new Uri(builder.Configuration["InventoryService:BaseUrl"] ?? "http://localhost:5233");
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
