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
            CreateMap<UpdateClientDto, UpdateClientRequest>()
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName ?? ""))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName ?? ""))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? ""))
                .ForMember(dest => dest.Username, opt => opt.MapFrom(src => src.Username ?? ""))
                .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.BirthDate ?? ""))
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address ?? ""))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber ?? ""))
                .ForMember(dest => dest.Password, opt => opt.MapFrom(src => src.Password ?? "")); 
        }
    }
}