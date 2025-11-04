using System.Reflection;
using ApiGatewayService.Filters;
using ApiGatewayService.Services.Implementation;
using ApiGatewayService.Services.Interfaces;
using Clients;

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
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddEndpointsApiExplorer();
var app = builder.Build();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
