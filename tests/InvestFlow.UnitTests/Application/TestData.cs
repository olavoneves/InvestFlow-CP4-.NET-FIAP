using System.Reflection;
using AutoMapper;
using InvestFlow.Application;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Enums;

namespace InvestFlow.UnitTests.Application;

/// <summary>Entidades e mapper para os testes da Application.</summary>
internal static class TestData
{
    public static readonly DateTime Data = new(2026, 3, 10, 14, 30, 0, DateTimeKind.Utc);

    public static IMapper CriarMapper() =>
        new MapperConfiguration(cfg => cfg.AddMaps(typeof(DependencyInjection).Assembly)).CreateMapper();

    public static Ativo CriarAtivo(int id = 1, string ticker = "PETR4", string nome = "Petrobras PN") =>
        ComId(new Ativo(ticker, nome, TipoAtivo.Acao, 38.12m), id);

    public static Ordem CriarOrdem(int id = 10, Ativo? ativo = null, int quantidade = 100, decimal preco = 32.5m)
    {
        ativo ??= CriarAtivo();
        var ordem = ComId(new Ordem(ativo.Id, LadoOrdem.Compra, quantidade, preco, Data), id);

        // Ordem.Ativo tem setter privado (preenchido pelo EF Core); simula a navegação carregada.
        typeof(Ordem).GetProperty(nameof(Ordem.Ativo))!.SetValue(ordem, ativo);
        return ordem;
    }

    // Id tem setter privado e é gerado pelo banco.
    public static T ComId<T>(T entidade, int id) where T : class
    {
        typeof(T).GetProperty("Id", BindingFlags.Public | BindingFlags.Instance)!.SetValue(entidade, id);
        return entidade;
    }
}
