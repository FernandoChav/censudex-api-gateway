using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ApiGatewayService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayService.Services.Implementation
{
    public class AuthProxyService(IHttpClientFactory httpClientFactory, ILogger<AuthProxyService> logger) : ControllerBase, IAuthProxyService
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly ILogger<AuthProxyService> _logger = logger;

        public async Task<IActionResult> ProxyLoginAsync(JsonElement body)
        {
            var client = _httpClientFactory.CreateClient("AuthServiceClient");
            _logger.LogInformation("Forwarding /login request to AuthService");

            var response = await client.PostAsJsonAsync("/auth/login", body);
            return await CreateProxyResponse(response);
        }

        public async Task<IActionResult> ProxyLogoutAsync(string? authorizationHeader)
        {
            var client = _httpClientFactory.CreateClient("AuthServiceClient");

            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                client.DefaultRequestHeaders.Authorization =
                    AuthenticationHeaderValue.Parse(authorizationHeader);
            }

            _logger.LogInformation("Forwarding /logout request to AuthService");
            var response = await client.PostAsync("/auth/logout", null);
            return await CreateProxyResponse(response);
        }

        public async Task<IActionResult> ProxyValidateTokenAsync(string? authorizationHeader)
        {
            var client = _httpClientFactory.CreateClient("AuthServiceClient");

            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                client.DefaultRequestHeaders.Authorization =
                    AuthenticationHeaderValue.Parse(authorizationHeader);
            }

            _logger.LogInformation("Forwarding /validate-token request to AuthService");
            var response = await client.GetAsync("/auth/validate-token");
            return await CreateProxyResponse(response);
        }

        // El método de ayuda ahora vive aquí, en el servicio
        private async Task<IActionResult> CreateProxyResponse(HttpResponseMessage response)
        {
            // Primero, revisa si la respuesta NO fue exitosa
            if (!response.IsSuccessStatusCode)
            {
                // Lee el cuerpo como texto plano (string), por si viene vacío
                var errorContent = await response.Content.ReadAsStringAsync();

                // Si el cuerpo del error estaba vacío, crea un mensaje genérico
                object errorBody = string.IsNullOrEmpty(errorContent)
                    ? new { message = (string?)response.ReasonPhrase } // ej: "Not Found"
                    : new { message = (string?)errorContent }; // o usa el mensaje de error

                return StatusCode((int)response.StatusCode, errorBody);
            }

            // Si SÍ fue exitosa (2xx), entonces es seguro leerlo como JSON
            var successContent = await response.Content.ReadFromJsonAsync<object>();
            return Ok(successContent);
        }
    }
}