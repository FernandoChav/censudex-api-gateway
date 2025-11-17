using System.Reflection;
using ApiGatewayService.Filters;
using ApiGatewayService.Services.Implementation;
using ApiGatewayService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ApiGatewayService.Middleware;
using Censudex_orders.Protos; // <-- FUSIONADO

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers(options =>
{
    options.Filters.Add<GrpcExceptionFilter>();
});


builder.Services.AddOpenApi();
builder.Services.AddHttpClient("ProductsService", client => 
{
    client.BaseAddress = new Uri(builder.Configuration["ProductsService:BaseUrl"] ?? "http://localhost:3001");
});
builder.Services.AddHttpClient("InventoryService", client => 
{
    client.BaseAddress = new Uri(builder.Configuration["InventoryService:BaseUrl"] ?? "http://localhost:5233");
});
builder.Services.AddGrpcClient<OrderService.OrderServiceClient>(options =>
{
    var orderServiceUrl = builder.Configuration["OrderService:BaseUrl"] ?? "http://localhost:5001";
    options.Address = new Uri(orderServiceUrl);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = 
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    return handler;
});


builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
        };
    });

builder.Services.AddGrpcClient<Clients.Clients.ClientsClient>(o =>
{
    var url = builder.Configuration.GetValue<string>("ServiceUrls:ClientsService");
    o.Address = new Uri(url!);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
    return handler;
});

builder.Services.AddHttpClient("AuthServiceClient", client =>
{
    var url = builder.Configuration["ServiceUrls:AuthService"];
    client.BaseAddress = new Uri(url!);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback =
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    return handler;
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IAuthProxyService, AuthProxyService>();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Pipeline (Este estaba bien)
app.UseHttpsRedirection();
app.UseMiddleware<JwtAuthorizationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();