using Biblioteca.Dominio.Contratos;

namespace Biblioteca.Dominio.Servicos;

/// <summary>
/// Casos de uso de Livro, incluindo as regras que cruzam o agregado Categoria.
/// Consumida tanto pelo <c>LivrosController</c> (REST) quanto pelo <c>LivroGrpcService</c> (gRPC).
/// </summary>
public interface ILivroService
{
    Task<IReadOnlyList<LivroDto>> ListarAsync(FiltroLivroDto? filtro = null, CancellationToken cancellationToken = default);

    Task<LivroDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Cadastra o livro. Falha se a categoria não existir ou estiver inativa (regra R1).</summary>
    Task<LivroDto> CriarAsync(CriarLivroDto dados, CancellationToken cancellationToken = default);

    /// <summary>Atualiza o livro. Ao trocar de categoria, a nova precisa estar ativa (regra R1).</summary>
    Task<LivroDto> AtualizarAsync(Guid id, AtualizarLivroDto dados, CancellationToken cancellationToken = default);

    /// <summary>Remove o livro. Falha se houver exemplares emprestados (regra R8).</summary>
    Task RemoverAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Empresta um exemplar. Exige categoria ativa e exemplar disponível (regras R7 e R6).</summary>
    Task<LivroDto> EmprestarAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LivroDto> DevolverAsync(Guid id, CancellationToken cancellationToken = default);
}
