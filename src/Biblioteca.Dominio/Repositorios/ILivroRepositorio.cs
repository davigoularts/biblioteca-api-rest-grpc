using Biblioteca.Dominio.Entidades;

namespace Biblioteca.Dominio.Repositorios;

public interface ILivroRepositorio
{
    Task<IReadOnlyList<Livro>> ListarAsync(Guid? categoriaId, string? termo, CancellationToken cancellationToken = default);

    Task<Livro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Livro?> ObterPorIsbnAsync(string isbn, CancellationToken cancellationToken = default);

    Task<int> ContarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);

    /// <summary>Contagem de livros por categoria, para montar a listagem sem N+1.</summary>
    Task<IReadOnlyDictionary<Guid, int>> ContarPorCategoriasAsync(CancellationToken cancellationToken = default);

    Task<bool> ExisteEmprestimoAtivoNaCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Livro livro, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Livro livro, CancellationToken cancellationToken = default);

    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
