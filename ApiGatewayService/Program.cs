using System.Reflection;
using ApiGatewayService.Filters;
using ApiGatewayService.Services.Implementation;
using ApiGatewayService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ApiGatewayService.Middleware;
var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers(options =>
{
    options.Filters.Add<GrpcExceptionFilter>();
});
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Dejamos esto vacío a propósito.
        // Tu JwtAuthorizationMiddleware y tu AuthProxyService
        // se encargan de la validación real.
        // Esto solo evita la excepción.
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


app.UseHttpsRedirection();

app.UseMiddleware<JwtAuthorizationMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
