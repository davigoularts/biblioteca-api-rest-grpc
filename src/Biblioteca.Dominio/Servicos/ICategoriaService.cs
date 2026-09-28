using Biblioteca.Dominio.Contratos;

namespace Biblioteca.Dominio.Servicos;

/// <summary>
/// Casos de uso de Categoria. É esta interface que o <c>CategoriasController</c> (REST)
/// e o <c>CategoriaGrpcService</c> (gRPC) consomem — nenhuma regra é reescrita nas bordas.
/// </summary>
public interface ICategoriaService
{
    Task<IReadOnlyList<CategoriaDto>> ListarAsync(bool apenasAtivas = false, CancellationToken cancellationToken = default);

    Task<CategoriaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CategoriaDto> CriarAsync(CriarCategoriaDto dados, CancellationToken cancellationToken = default);

    Task<CategoriaDto> AtualizarAsync(Guid id, AtualizarCategoriaDto dados, CancellationToken cancellationToken = default);

    Task<CategoriaDto> AtivarAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Inativa a categoria. Falha se houver livros com exemplares emprestados (regra R2).</summary>
    Task<CategoriaDto> InativarAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Remove a categoria. Falha se houver livros vinculados (regra R3).</summary>
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);
}
