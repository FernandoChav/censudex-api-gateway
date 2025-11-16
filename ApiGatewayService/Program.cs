using System.Reflection;
using ApiGatewayService.Filters;
using ApiGatewayService.Services.Implementation;
using ApiGatewayService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer; 
using Microsoft.IdentityModel.Tokens; 
using System.IdentityModel.Tokens.Jwt;
var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers(options =>
{
    options.Filters.Add<GrpcExceptionFilter>();
});
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());


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
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = false,
        SignatureValidator = (token, parameters) => new JwtSecurityToken(token) 
    };
});

builder.Services.AddAuthorization();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();


app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
