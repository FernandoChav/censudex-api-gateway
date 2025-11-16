using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using ApiGatewayService.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayService.Middleware
{
    public class JwtAuthorizationMiddleware(RequestDelegate next, ILogger<JwtAuthorizationMiddleware> logger)
    {
        private readonly RequestDelegate _next = next;
        private readonly ILogger<JwtAuthorizationMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext context, IAuthProxyService authProxy)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
            {
                await _next(context);
                return;
            }

            var allowAnonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>();
            if (allowAnonymous != null)
            {
                await _next(context);
                return;
            }
            var authorizeData = endpoint.Metadata.GetMetadata<IAuthorizeData>();

            if (authorizeData != null)
            {
                _logger.LogInformation("Protected endpoint hit. Validating token...");

                context.Request.Headers.TryGetValue("Authorization", out var authHeader);

                if (string.IsNullOrEmpty(authHeader))
                {
                    _logger.LogWarning("Authorization header missing");
                    context.Response.StatusCode = 401; // Unauthorized
                    await context.Response.WriteAsJsonAsync(new { message = "Authorization header is missing" });
                    return;
                }

                var result = await authProxy.ProxyValidateTokenAsync(authHeader);

                if (result is ObjectResult objectResult && objectResult.StatusCode >= 400)
                {
                    _logger.LogWarning("Token validation failed by AuthService");
                    context.Response.StatusCode = objectResult.StatusCode.Value;
                    await context.Response.WriteAsJsonAsync(objectResult.Value);
                    return;
                }

                _logger.LogInformation("Token validated by proxy. Building ClaimsPrincipal.");
                var claims = new List<Claim>
                {

                    new Claim("auth_via", "api_gateway_proxy")
                };

                if (result is OkObjectResult okResult && okResult.Value != null)
                {
                    try
                    {

                        var jsonValue = JsonSerializer.Serialize(okResult.Value);
                        _logger.LogInformation("Respuesta del proxy (JSON): {jsonValue}", jsonValue);
                        var claimsData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonValue);

                        if (claimsData != null)
                        {

                            if (claimsData.TryGetValue("sub", out var userId) && userId.ValueKind != JsonValueKind.Null)
                            {
                                var userIdString = userId.ValueKind == JsonValueKind.String ? userId.GetString() : userId.ToString();
                                if (!string.IsNullOrEmpty(userIdString))
                                {

                                    claims.Add(new Claim(ClaimTypes.NameIdentifier, userIdString));
                                }
                            }


                            if (claimsData.TryGetValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var role) && role.ValueKind != JsonValueKind.Null)
                            {
                                var roleString = role.ValueKind == JsonValueKind.String ? role.GetString() : role.ToString();
                                if (!string.IsNullOrEmpty(roleString))
                                {

                                    claims.Add(new Claim(ClaimTypes.Role, roleString));
                                }
                            }

                            else if (claimsData.TryGetValue("role", out var shortRole) && shortRole.ValueKind != JsonValueKind.Null)
                            {
                                var roleString = shortRole.ValueKind == JsonValueKind.String ? shortRole.GetString() : shortRole.ToString();
                                if (!string.IsNullOrEmpty(roleString))
                                {
                                    claims.Add(new Claim(ClaimTypes.Role, roleString));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not parse claims from proxy response. Using generic identity.");
                    }
                }

                var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                context.User = new ClaimsPrincipal(identity);
                _logger.LogInformation("--- DEBUG INFO USUARIO ---");
                _logger.LogInformation("context.User.Identity.IsAuthenticated: {isAuthenticated}", context.User.Identity.IsAuthenticated);
                var claimsLog = context.User.Claims.Select(c => $"Tipo: {c.Type} | Valor: {c.Value}");
                _logger.LogInformation("Claims en el contexto: [\n  {claims}\n]", string.Join("\n  ", claimsLog));
                _logger.LogInformation("--- FIN DEBUG INFO ---");
            }

            // Llama al siguiente middleware
            await _next(context);
        }
    }
}