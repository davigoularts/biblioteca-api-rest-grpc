namespace Biblioteca.Dominio.Contratos;

/// <summary>Representação de saída de um livro, já com o nome da categoria resolvido.</summary>
public sealed record LivroDto(
    Guid Id,
    string Titulo,
    string Autor,
    string Isbn,
    int AnoPublicacao,
    Guid CategoriaId,
    string CategoriaNome,
    bool CategoriaAtiva,
    int TotalExemplares,
    int ExemplaresEmprestados,
    int ExemplaresDisponiveis,
    DateTime CadastradoEm);

public sealed record CriarLivroDto(
    string? Titulo,
    string? Autor,
    string? Isbn,
    int AnoPublicacao,
    Guid CategoriaId,
    int TotalExemplares);

public sealed record AtualizarLivroDto(
    string? Titulo,
    string? Autor,
    string? Isbn,
    int AnoPublicacao,
    Guid CategoriaId,
    int TotalExemplares);

/// <summary>Filtros de listagem de livros.</summary>
public sealed record FiltroLivroDto(Guid? CategoriaId = null, string? Termo = null);
