using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiGatewayService.Dtos.Clients;
using ApiGatewayService.Services.Interfaces;
using AutoMapper;
using Clients;
using Grpc.Core;

namespace ApiGatewayService.Services.Implementation
{
    public class ClientService(Clients.Clients.ClientsClient gRpcClient, IMapper mapper) : IClientService
    {
        private readonly Clients.Clients.ClientsClient _gRpcClient = gRpcClient;
        private readonly IMapper _mapper = mapper;

        public async Task<ClientResponse> CreateClientAsync(CreateClientDto createClientDto)
        {
            var request = _mapper.Map<CreateClientRequest>(createClientDto);
            return await _gRpcClient.CreateClientAsync(request);
        }

        public async Task<DeleteClientResponse> DeleteClientAsync(string clientId)
        {
            var request = new DeleteClientRequest { Id = clientId };
            return await _gRpcClient.DeleteClientAsync(request);
        }

        public async Task<IEnumerable<ClientResponse>> GetAllClientsAsync(string? status, string? name, string? email, string? username)
        {
            var request = new GetAllClientsRequest
            {
                FilterStatus = status ?? "",
                FilterName = name ?? "",
                FilterEmail = email ?? "",
                FilterUsername = username ?? ""
            };
            var response = await _gRpcClient.GetAllClientsAsync(request);
            return response.Clients;
        }

        public async Task<ClientResponse> GetClientByIdAsync(string clientId)
        {
            var request = new GetClientByIdRequest { Id = clientId };
            return await _gRpcClient.GetClientByIdAsync(request);
        }

        public async Task<ClientResponse> UpdateClientAsync(string clientId, UpdateClientDto dto)
        {
            var request = _mapper.Map<UpdateClientRequest>(dto);
            request.Id = clientId;
            return await _gRpcClient.UpdateClientAsync(request);
        }
    }
}