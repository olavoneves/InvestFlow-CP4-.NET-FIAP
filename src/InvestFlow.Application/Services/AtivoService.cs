using AutoMapper;
using InvestFlow.Application.Common;
using InvestFlow.Application.DTOs.Ativos;
using InvestFlow.Application.Exceptions;
using InvestFlow.Application.Validation;
using InvestFlow.Domain.Entities;
using InvestFlow.Domain.Exceptions;
using InvestFlow.Domain.Filters;
using InvestFlow.Domain.Repositories;

namespace InvestFlow.Application.Services;

public class AtivoService : IAtivoService
{
    private readonly IAtivoRepository _ativoRepository;
    private readonly IOrdemRepository _ordemRepository;
    private readonly IMapper _mapper;

    public AtivoService(IAtivoRepository ativoRepository, IOrdemRepository ordemRepository, IMapper mapper)
    {
        _ativoRepository = ativoRepository;
        _ordemRepository = ordemRepository;
        _mapper = mapper;
    }

    public async Task<PagedResult<AtivoResponse>> GetPagedAsync(AtivoQuery query, CancellationToken cancellationToken = default)
    {
        RequestValidator.Validar(query);

        var filtro = new AtivoFiltro(Busca: query.Busca, Tipo: query.Tipo);
        var (items, totalCount) = await _ativoRepository.GetPagedAsync(
            query.PageNumber, query.PageSize, filtro, cancellationToken);

        return new PagedResult<AtivoResponse>(
            _mapper.Map<List<AtivoResponse>>(items), query.PageNumber, query.PageSize, totalCount);
    }

    public async Task<AtivoResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var ativo = await _ativoRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NaoEncontrado(id);

        return _mapper.Map<AtivoResponse>(ativo);
    }

    public async Task<AtivoResponse> CreateAsync(AtivoCreateRequest request, CancellationToken cancellationToken = default)
    {
        RequestValidator.Validar(request);

        // O índice único no banco é a garantia final; a checagem aqui devolve um erro de validação legível.
        if (await _ativoRepository.TickerExistsAsync(request.Ticker, cancellationToken))
        {
            throw new ValidationException(
                nameof(AtivoCreateRequest.Ticker),
                $"Já existe um ativo com o ticker '{request.Ticker.Trim().ToUpperInvariant()}'.");
        }

        var ativo = new Ativo(request.Ticker, request.Nome, request.Tipo, request.PrecoAtual);
        await _ativoRepository.AddAsync(ativo, cancellationToken);

        return _mapper.Map<AtivoResponse>(ativo);
    }

    public async Task<AtivoResponse> UpdateAsync(int id, AtivoUpdateRequest request, CancellationToken cancellationToken = default)
    {
        RequestValidator.Validar(request);

        var ativo = await _ativoRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw NaoEncontrado(id);

        ativo.Atualizar(request.Nome, request.Tipo, request.PrecoAtual);
        await _ativoRepository.UpdateAsync(ativo, cancellationToken);

        return _mapper.Map<AtivoResponse>(ativo);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var ativo = await _ativoRepository.GetByIdForUpdateAsync(id, cancellationToken)
            ?? throw NaoEncontrado(id);

        // A FK é Restrict: sem esta checagem a exclusão falharia no banco com erro genérico.
        if (await _ordemRepository.ExistsByAtivoIdAsync(id, cancellationToken))
            throw new DomainException("Não é possível excluir um ativo que possui ordens.");

        await _ativoRepository.DeleteAsync(ativo, cancellationToken);
    }

    private static NotFoundException NaoEncontrado(int id) => new($"Ativo com id {id} não foi encontrado.");
}
