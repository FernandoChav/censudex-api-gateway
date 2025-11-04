using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApiGatewayService.Dtos.Clients;
using AutoMapper;
using Clients;

namespace ApiGatewayService.Profiles
{
    public class ClientProfile : Profile
    {
        public ClientProfile()
        {
            CreateMap<CreateClientDto, CreateClientRequest>();
            CreateMap<UpdateClientDto, UpdateClientRequest>();
        }
    }
}