using AutoMapper;
using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ordens;
using InvestFlow.Application.Exceptions;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Filters;
using InvestFlow.Domain.Repositories;

namespace InvestFlow.Application.Services;

public class OrdemService : IOrdemService
{
    private readonly IOrdemRepository _ordemRepository;
    private readonly IAtivoRepository _ativoRepository;
    private readonly IMapper _mapper;

    public OrdemService(IOrdemRepository ordemRepository, IAtivoRepository ativoRepository, IMapper mapper)
    {
        _ordemRepository = ordemRepository;
        _ativoRepository = ativoRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<OrdemResponse>> GetPagedAsync(OrdemQuery query, CancellationToken cancellationToken = default)
    {
        RequestValidator.Validar(query);

        var filtro = new OrdemFiltro(
            AtivoId: query.AtivoId,
            Status: query.Status,
            DataInicio: query.DataInicio,
            DataFim: query.DataFim);

        var (items, totalCount) = await _ordemRepository.GetPagedAsync(
            query.PageNumber, query.PageSize, filtro, cancellationToken);

        return new PagedResult<OrdemResponse>(
            _mapper.Map<List<OrdemResponse>>(items), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<OrdemResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var ordem = await _ordemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NaoEncontrada(id);

        return _mapper.Map<OrdemResponse>(ordem);
    }

    public async Task<OrdemResponse> CreateAsync(OrdemCreateRequest request, CancellationToken cancellationToken = default)
    {
        RequestValidator.Validar(request);

        // O ativo vem no corpo da requisição: inexistente é dado inválido (400), não recurso ausente (404).
        if (!await _ativoRepository.ExistsAsync(request.AtivoId, cancellationToken))
        {
            throw new ValidationException(
                nameof(OrdemCreateRequest.AtivoId),
                $"Ativo com id {request.AtivoId} não foi encontrado.");
        }

        var ordem = new Ordem(
            request.AtivoId,
            request.Lado,
            request.Quantidade,
            request.PrecoExecucao,
            request.DataExecucao ?? DateTime.UtcNow);

        await _ordemRepository.AddAsync(ordem, cancellationToken);

        // Relê com o ativo carregado para preencher NomeAtivo.
        return await GetByIdAsync(ordem.Id, cancellationToken);
    }

    public Task<OrdemResponse> ExecutarAsync(int id, CancellationToken cancellationToken = default) =>
        AlterarStatusAsync(id, ordem => ordem.Executar(), cancellationToken);

    public Task<OrdemResponse> CancelarAsync(int id, CancellationToken cancellationToken = default) =>
        AlterarStatusAsync(id, ordem => ordem.Cancelar(), cancellationToken);

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var ordem = await _ordemRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw NaoEncontrada(id);

        await _ordemRepository.DeleteAsync(ordem, cancellationToken);
    }

    private async Task<OrdemResponse> AlterarStatusAsync(int id, Action<Ordem> transicao, CancellationToken cancellationToken)
    {
        var ordem = await _ordemRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw NaoEncontrada(id);

        // A entidade valida a transição e lança DomainException se não for permitida.
        transicao(ordem);
        await _ordemRepository.UpdateAsync(ordem, cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    private static NotFoundException NaoEncontrada(int id) => new($"Ordem com id {id} não foi encontrada.");
}
