using AutoMapper;
using InvestFlow.Application.DTOs.Ordens;
using InvestFlow.Domain.Entities;

namespace InvestFlow.Application.Mappings;

public class OrdemProfile : Profile
{
    public OrdemProfile()
    {
        CreateMap<Ordem, OrdemResponse>()
            // A convenção de achatamento do AutoMapper só reconheceria "AtivoNome".
            .ForMember(dest => dest.NomeAtivo, opt => opt.MapFrom(src => src.Ativo != null ? src.Ativo.Nome : string.Empty))
            // Propriedade calculada da entidade (não é persistida).
            .ForMember(dest => dest.ValorFinanceiro, opt => opt.MapFrom(src => src.ValorFinanceiro));
    }
}
