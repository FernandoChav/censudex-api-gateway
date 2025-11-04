using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiGatewayService.Dtos.Clients;
using Clients;

namespace ApiGatewayService.Services.Interfaces
{
    public interface IClientService
    {
        Task<ClientResponse> CreateClientAsync(CreateClientDto createClientDto);
        Task<ClientResponse> UpdateClientAsync(string id, UpdateClientDto dto);
        Task<IEnumerable<ClientResponse>> GetAllClientsAsync(string? status, string? name, string? email, string? username);
        Task<ClientResponse> GetClientByIdAsync(string clientId);
        Task<DeleteClientResponse> DeleteClientAsync(string clientId);
    }
}