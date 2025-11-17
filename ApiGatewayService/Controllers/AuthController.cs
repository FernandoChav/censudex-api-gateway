using System.Text.Json;
using ApiGatewayService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApiGatewayService.Controllers
{
    [ApiController]
    public class AuthController(IAuthProxyService authService) : BaseController
    {
        // Solo inyectamos nuestro servicio
        private readonly IAuthProxyService _authService = authService;

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] JsonElement body)
        {
            // El controlador solo coordina. ¡Cero lógica!
            return await _authService.ProxyLoginAsync(body);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Pasa la cabecera al servicio
            Request.Headers.TryGetValue("Authorization", out var authHeader);
            return await _authService.ProxyLogoutAsync(authHeader);
        }

        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken()
        {
            // Pasa la cabecera al servicio
            Request.Headers.TryGetValue("Authorization", out var authHeader);
            return await _authService.ProxyValidateTokenAsync(authHeader);
        }
    }
}