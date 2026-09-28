using Biblioteca.Dominio.Contratos;
using Biblioteca.Dominio.Entidades;
using Biblioteca.Dominio.Excecoes;
using Biblioteca.Dominio.Repositorios;

namespace Biblioteca.Dominio.Servicos;

/// <summary>
/// Único lugar onde as regras de Livro vivem. REST e gRPC apenas traduzem entrada/saída.
/// </summary>
public sealed class LivroService : ILivroService
{
    private const string NomeDoRecurso = "Livro";

    private readonly ILivroRepositorio _livros;
    private readonly ICategoriaRepositorio _categorias;

    public LivroService(ILivroRepositorio livros, ICategoriaRepositorio categorias)
    {
        _livros = livros;
        _categorias = categorias;
    }

    public async Task<IReadOnlyList<LivroDto>> ListarAsync(
        FiltroLivroDto? filtro = null,
        CancellationToken cancellationToken = default)
    {
        filtro ??= new FiltroLivroDto();

        var livros = await _livros.ListarAsync(filtro.CategoriaId, filtro.Termo, cancellationToken);
        var categorias = (await _categorias.ListarAsync(apenasAtivas: false, cancellationToken))
            .ToDictionary(categoria => categoria.Id);

        return livros
            .Select(livro => Mapear(livro, categorias.GetValueOrDefault(livro.CategoriaId)))
            .ToList();
    }

    public async Task<LivroDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var livro = await BuscarObrigatorioAsync(id, cancellationToken);
        var categoria = await _categorias.ObterPorIdAsync(livro.CategoriaId, cancellationToken);
        return Mapear(livro, categoria);
    }

    public async Task<LivroDto> CriarAsync(CriarLivroDto dados, CancellationToken cancellationToken = default)
    {
        GarantirCorpo(dados);

        var livro = Livro.Criar(
            dados.Titulo,
            dados.Autor,
            dados.Isbn,
            dados.AnoPublicacao,
            dados.CategoriaId,
            dados.TotalExemplares);

        // R1 - a regra central: só é possível vincular livro a categoria existente e ativa.
        var categoria = await BuscarCategoriaAtivaAsync(livro.CategoriaId, cancellationToken);

        // R4 - ISBN é único no acervo.
        await GarantirIsbnUnicoAsync(livro.Isbn, idAtual: null, cancellationToken);

        await _livros.AdicionarAsync(livro, cancellationToken);
        return Mapear(livro, categoria);
    }

    public async Task<LivroDto> AtualizarAsync(
        Guid id,
        AtualizarLivroDto dados,
        CancellationToken cancellationToken = default)
    {
        GarantirCorpo(dados);

        if (dados.CategoriaId == Guid.Empty)
        {
            // Sem isso a busca abaixo devolveria 404 no lugar de um 400 de campo obrigatório.
            throw new ValidacaoException(
                "Dados inválidos para o livro.",
                new Dictionary<string, string[]> { ["categoriaId"] = ["A categoria é obrigatória."] });
        }

        var livro = await BuscarObrigatorioAsync(id, cancellationToken);

        // Toda checagem acontece antes de mutar a entidade.
        var trocouDeCategoria = dados.CategoriaId != livro.CategoriaId;

        var categoria = trocouDeCategoria
            // R1 - ao mover o livro, a categoria de destino precisa estar ativa.
            ? await BuscarCategoriaAtivaAsync(dados.CategoriaId, cancellationToken)
            : await _categorias.ObterPorIdAsync(livro.CategoriaId, cancellationToken);

        await GarantirIsbnUnicoAsync(Livro.NormalizarIsbn(dados.Isbn), livro.Id, cancellationToken);

        livro.Atualizar(
            dados.Titulo,
            dados.Autor,
            dados.Isbn,
            dados.AnoPublicacao,
            dados.CategoriaId,
            dados.TotalExemplares);

        await _livros.AtualizarAsync(livro, cancellationToken);
        return Mapear(livro, categoria);
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var livro = await BuscarObrigatorioAsync(id, cancellationToken);

        // R8 - não se descarta livro com exemplar na mão do leitor.
        if (livro.PossuiEmprestimoAtivo)
        {
            throw new RegraDeNegocioException(
                $"O livro '{livro.Titulo}' possui {livro.ExemplaresEmprestados} exemplar(es) emprestado(s) e não pode ser removido.",
                "LIVRO_COM_EMPRESTIMO_ATIVO");
        }

        await _livros.RemoverAsync(id, cancellationToken);
    }

    public async Task<LivroDto> EmprestarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var livro = await BuscarObrigatorioAsync(id, cancellationToken);

        // R7 - outra regra entre agregados: acervo de categoria inativa não circula.
        var categoria = await BuscarCategoriaAtivaAsync(
            livro.CategoriaId,
            cancellationToken,
            $"Empréstimos estão suspensos: a categoria do livro '{livro.Titulo}' está inativa.");

        livro.Emprestar();
        await _livros.AtualizarAsync(livro, cancellationToken);

        return Mapear(livro, categoria);
    }

    public async Task<LivroDto> DevolverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var livro = await BuscarObrigatorioAsync(id, cancellationToken);

        // Devolução é sempre permitida, inclusive em categoria inativa.
        livro.Devolver();
        await _livros.AtualizarAsync(livro, cancellationToken);

        var categoria = await _categorias.ObterPorIdAsync(livro.CategoriaId, cancellationToken);
        return Mapear(livro, categoria);
    }

    private async Task<Livro> BuscarObrigatorioAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ValidacaoException(
                "Identificador inválido.",
                new Dictionary<string, string[]> { ["id"] = ["O id do livro é obrigatório."] });
        }

        return await _livros.ObterPorIdAsync(id, cancellationToken)
               ?? throw new RecursoNaoEncontradoException(NomeDoRecurso, id);
    }

    private async Task<Categoria> BuscarCategoriaAtivaAsync(
        Guid categoriaId,
        CancellationToken cancellationToken,
        string? mensagemQuandoInativa = null)
    {
        var categoria = await _categorias.ObterPorIdAsync(categoriaId, cancellationToken)
                        ?? throw new RecursoNaoEncontradoException("Categoria", categoriaId);

        if (!categoria.Ativa)
        {
            throw new RegraDeNegocioException(
                mensagemQuandoInativa
                ?? $"A categoria '{categoria.Nome}' está inativa; não é possível vincular livros a ela.",
                "LIVRO_CATEGORIA_INATIVA");
        }

        return categoria;
    }

    private async Task GarantirIsbnUnicoAsync(string isbn, Guid? idAtual, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return; // a entidade já reporta o campo obrigatório.
        }

        var existente = await _livros.ObterPorIsbnAsync(isbn, cancellationToken);
        if (existente is not null && existente.Id != idAtual)
        {
            throw new RecursoDuplicadoException("livro", "ISBN", isbn);
        }
    }

    private static void GarantirCorpo(object? dados)
    {
        if (dados is null)
        {
            throw new ValidacaoException("O corpo da requisição é obrigatório.");
        }
    }

    private static LivroDto Mapear(Livro livro, Categoria? categoria) => new(
        livro.Id,
        livro.Titulo,
        livro.Autor,
        livro.Isbn,
        livro.AnoPublicacao,
        livro.CategoriaId,
        categoria?.Nome ?? string.Empty,
        categoria?.Ativa ?? false,
        livro.TotalExemplares,
        livro.ExemplaresEmprestados,
        livro.ExemplaresDisponiveis,
        livro.CadastradoEm);
}
