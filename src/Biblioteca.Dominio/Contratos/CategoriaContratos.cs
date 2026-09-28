namespace Biblioteca.Dominio.Contratos;

/// <summary>Representação de saída de uma categoria, compartilhada por REST e gRPC.</summary>
public sealed record CategoriaDto(
    Guid Id,
    string Nome,
    string Descricao,
    bool Ativa,
    DateTime CriadaEm,
    int TotalLivros);

/// <summary>
/// Entrada de criação. Campos anuláveis de propósito: quem valida é o domínio,
/// não anotações da camada web (assim REST e gRPC recebem exatamente o mesmo tratamento).
/// </summary>
public sealed record CriarCategoriaDto(string? Nome, string? Descricao, bool Ativa = true);

public sealed record AtualizarCategoriaDto(string? Nome, string? Descricao);
