using ApiGatewayService.Dtos.Clients;
using ApiGatewayService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
namespace ApiGatewayService.Controllers
{   
    [ApiController]
    [Authorize]
    public class ClientsController(IClientService clientService) : BaseController
    {
        private readonly IClientService _clientService = clientService;

        [HttpPost("create")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateClient([FromBody] CreateClientDto request)
        {
            var response = await _clientService.CreateClientAsync(request);
            return Ok(response);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetClient(string id)
        {
            var response = await _clientService.GetClientByIdAsync(id);
            return Ok(response);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllClients([FromQuery] string? filter_status, [FromQuery] string? filter_name, [FromQuery] string? filter_email, [FromQuery] string? filter_username)
        {
            var response = await _clientService.GetAllClientsAsync(filter_status, filter_name, filter_email, filter_username);
            return Ok(response);
        }
        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateClient(string id, [FromBody] UpdateClientDto request)
        {
            // Tu ClientService ya tiene el método UpdateClientAsync listo, solo faltaba exponerlo aquí
            var response = await _clientService.UpdateClientAsync(id, request);
            return Ok(response);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClient(string id)
        {
            var response = await _clientService.DeleteClientAsync(id);
            return Ok(response);
        }
    }
}