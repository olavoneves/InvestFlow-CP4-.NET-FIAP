using System.ComponentModel.DataAnnotations;

namespace InvestFlow.Application.Common;

/// <summary>Parâmetros de paginação comuns a todas as listagens.</summary>
public class PageRequest
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    // Teto que mantém (PageNumber - 1) * PageSize dentro de int no Skip do repositório.
    public const int MaxPageNumber = int.MaxValue / MaxPageSize;

    /// <summary>Número da página, a partir de 1.</summary>
    [Range(1, MaxPageNumber, ErrorMessage = "PageNumber deve estar entre {1} e {2}.")]
    public int PageNumber { get; set; } = DefaultPageNumber;

    /// <summary>Quantidade de itens por página (máximo 50).</summary>
    [Range(1, MaxPageSize, ErrorMessage = "PageSize deve estar entre {1} e {2}.")]
    public int PageSize { get; set; } = DefaultPageSize;
}
