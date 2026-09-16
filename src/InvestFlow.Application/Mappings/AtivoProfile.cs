using AutoMapper;
using InvestFlow.Application.DTOs.Ativos;
using InvestFlow.Domain.Entities;

namespace InvestFlow.Application.Mappings;

public class AtivoProfile : Profile
{
    public AtivoProfile()
    {
        // Só entidade -> response: a criação e a alteração passam pelo construtor e pelos
        // métodos da entidade, que aplicam as regras de negócio.
        CreateMap<Ativo, AtivoResponse>();
    }
}
