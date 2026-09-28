using Biblioteca.Dominio.Entidades;
using Biblioteca.Dominio.Repositorios;

namespace Biblioteca.Repositorio.Memoria;

public sealed class LivroRepositorioEmMemoria : ILivroRepositorio
{
    private readonly BancoEmMemoria _banco;

    public LivroRepositorioEmMemoria(BancoEmMemoria banco) => _banco = banco;

    public Task<IReadOnlyList<Livro>> ListarAsync(
        Guid? categoriaId,
        string? termo,
        CancellationToken cancellationToken = default)
    {
        var consulta = _banco.Livros.Values.AsEnumerable();

        if (categoriaId is { } id && id != Guid.Empty)
        {
            consulta = consulta.Where(livro => livro.CategoriaId == id);
        }

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var busca = termo.Trim();
            consulta = consulta.Where(livro =>
                livro.Titulo.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                livro.Autor.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                livro.Isbn.Contains(busca, StringComparison.OrdinalIgnoreCase));
        }

        IReadOnlyList<Livro> resultado = consulta
            .OrderBy(livro => livro.Titulo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(resultado);
    }

    public Task<Livro?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_banco.Livros.TryGetValue(id, out var livro) ? livro : null);

    public Task<Livro?> ObterPorIsbnAsync(string isbn, CancellationToken cancellationToken = default)
    {
        var encontrado = _banco.Livros.Values
            .FirstOrDefault(livro => string.Equals(livro.Isbn, isbn, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(encontrado);
    }

    public Task<int> ContarPorCategoriaAsync(Guid categoriaId, CancellationToken cancellationToken = default)
        => Task.FromResult(_banco.Livros.Values.Count(livro => livro.CategoriaId == categoriaId));

    public Task<IReadOnlyDictionary<Guid, int>> ContarPorCategoriasAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<Guid, int> totais = _banco.Livros.Values
            .GroupBy(livro => livro.CategoriaId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());

        return Task.FromResult(totais);
    }

    public Task<bool> ExisteEmprestimoAtivoNaCategoriaAsync(
        Guid categoriaId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(_banco.Livros.Values
            .Any(livro => livro.CategoriaId == categoriaId && livro.PossuiEmprestimoAtivo));

    public Task AdicionarAsync(Livro livro, CancellationToken cancellationToken = default)
    {
        _banco.Livros[livro.Id] = livro;
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(Livro livro, CancellationToken cancellationToken = default)
    {
        _banco.Livros[livro.Id] = livro;
        return Task.CompletedTask;
    }

    public Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _banco.Livros.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
