using Biblioteca.Dominio.Entidades;
using Biblioteca.Dominio.Repositorios;

namespace Biblioteca.Repositorio.Memoria;

public sealed class CategoriaRepositorioEmMemoria : ICategoriaRepositorio
{
    private readonly BancoEmMemoria _banco;

    public CategoriaRepositorioEmMemoria(BancoEmMemoria banco) => _banco = banco;

    public Task<IReadOnlyList<Categoria>> ListarAsync(bool apenasAtivas, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Categoria> resultado = _banco.Categorias.Values
            .Where(categoria => !apenasAtivas || categoria.Ativa)
            .OrderBy(categoria => categoria.Nome, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(resultado);
    }

    public Task<Categoria?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_banco.Categorias.TryGetValue(id, out var categoria) ? categoria : null);

    public Task<Categoria?> ObterPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        var encontrada = _banco.Categorias.Values
            .FirstOrDefault(categoria => string.Equals(categoria.Nome, nome, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(encontrada);
    }

    public Task AdicionarAsync(Categoria categoria, CancellationToken cancellationToken = default)
    {
        _banco.Categorias[categoria.Id] = categoria;
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(Categoria categoria, CancellationToken cancellationToken = default)
    {
        _banco.Categorias[categoria.Id] = categoria;
        return Task.CompletedTask;
    }

    public Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _banco.Categorias.TryRemove(id, out _);
        return Task.CompletedTask;
    }
}
