using Biblioteca.Dominio.Entidades;

namespace Biblioteca.Dominio.Repositorios;

/// <summary>
/// Porta de saída do domínio para a camada de repositório.
/// A implementação (memória, EF Core, Dapper...) vive fora do domínio.
/// </summary>
public interface ICategoriaRepositorio
{
    Task<IReadOnlyList<Categoria>> ListarAsync(bool apenasAtivas, CancellationToken cancellationToken = default);

    Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Categoria?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Categoria categoria, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Categoria categoria, CancellationToken cancellationToken = default);

    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
