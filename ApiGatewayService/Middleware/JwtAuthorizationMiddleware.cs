using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiGatewayService.Services.Interfaces;
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
            var authorizeData = endpoint?.Metadata.GetMetadata<IAuthorizeData>();

            if (authorizeData != null)
            {

                _logger.LogInformation("Protected endpoint hit. Validating token...");


                context.Request.Headers.TryGetValue("Authorization", out var authHeader);

                if (string.IsNullOrEmpty(authHeader))
                {
                    _logger.LogWarning("Authorization header missing");
                    context.Response.StatusCode = 401;
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

                _logger.LogInformation("Token validated successfully");
            }


            await _next(context);
        }
    }
}