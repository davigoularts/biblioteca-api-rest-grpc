using Biblioteca.Dominio.Contratos;
using Biblioteca.Dominio.Entidades;
using Biblioteca.Dominio.Excecoes;
using Biblioteca.Dominio.Repositorios;

namespace Biblioteca.Dominio.Servicos;

public sealed class CategoriaService : ICategoriaService
{
    private const string NomeDoRecurso = "Categoria";

    private readonly ICategoriaRepositorio _categorias;
    private readonly ILivroRepositorio _livros;

    public CategoriaService(ICategoriaRepositorio categorias, ILivroRepositorio livros)
    {
        _categorias = categorias;
        _livros = livros;
    }

    public async Task<IReadOnlyList<CategoriaDto>> ListarAsync(
        bool apenasAtivas = false,
        CancellationToken cancellationToken = default)
    {
        var categorias = await _categorias.ListarAsync(apenasAtivas, cancellationToken);
        var totais = await _livros.ContarPorCategoriasAsync(cancellationToken);

        return categorias
            .Select(categoria => Mapear(categoria, totais.TryGetValue(categoria.Id, out var total) ? total : 0))
            .ToList();
    }

    public async Task<CategoriaDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await BuscarObrigatoriaAsync(id, cancellationToken);
        return Mapear(categoria, await _livros.ContarPorCategoriaAsync(id, cancellationToken));
    }

    public async Task<CategoriaDto> CriarAsync(CriarCategoriaDto dados, CancellationToken cancellationToken = default)
    {
        GarantirCorpo(dados);

        // A entidade valida o formato; o serviço valida o que depende de consultar o repositório.
        var categoria = Categoria.Criar(dados.Nome, dados.Descricao, dados.Ativa);
        await GarantirNomeUnicoAsync(categoria.Nome, idAtual: null, cancellationToken);

        await _categorias.AdicionarAsync(categoria, cancellationToken);
        return Mapear(categoria, totalLivros: 0);
    }

    public async Task<CategoriaDto> AtualizarAsync(
        Guid id,
        AtualizarCategoriaDto dados,
        CancellationToken cancellationToken = default)
    {
        GarantirCorpo(dados);

        var categoria = await BuscarObrigatoriaAsync(id, cancellationToken);
        await GarantirNomeUnicoAsync(Categoria.NormalizarNome(dados.Nome), categoria.Id, cancellationToken);

        categoria.Atualizar(dados.Nome, dados.Descricao);
        await _categorias.AtualizarAsync(categoria, cancellationToken);

        return Mapear(categoria, await _livros.ContarPorCategoriaAsync(id, cancellationToken));
    }

    public async Task<CategoriaDto> AtivarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await BuscarObrigatoriaAsync(id, cancellationToken);

        categoria.Ativar();
        await _categorias.AtualizarAsync(categoria, cancellationToken);

        return Mapear(categoria, await _livros.ContarPorCategoriaAsync(id, cancellationToken));
    }

    public async Task<CategoriaDto> InativarAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await BuscarObrigatoriaAsync(id, cancellationToken);

        // R2 - regra entre agregados: não se inativa um acervo com empréstimo em aberto.
        if (categoria.Ativa && await _livros.ExisteEmprestimoAtivoNaCategoriaAsync(id, cancellationToken))
        {
            throw new RegraDeNegocioException(
                $"A categoria '{categoria.Nome}' possui livros com exemplares emprestados e não pode ser inativada.",
                "CATEGORIA_COM_EMPRESTIMO_ATIVO");
        }

        categoria.Inativar();
        await _categorias.AtualizarAsync(categoria, cancellationToken);

        return Mapear(categoria, await _livros.ContarPorCategoriaAsync(id, cancellationToken));
    }

    public async Task RemoverAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var categoria = await BuscarObrigatoriaAsync(id, cancellationToken);

        // R3 - regra entre agregados: categoria com acervo vinculado não pode ser apagada.
        var totalLivros = await _livros.ContarPorCategoriaAsync(id, cancellationToken);
        if (totalLivros > 0)
        {
            throw new RegraDeNegocioException(
                $"A categoria '{categoria.Nome}' possui {totalLivros} livro(s) vinculado(s) e não pode ser removida.",
                "CATEGORIA_COM_LIVROS_VINCULADOS");
        }

        await _categorias.RemoverAsync(id, cancellationToken);
    }

    private async Task<Categoria> BuscarObrigatoriaAsync(Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty)
        {
            throw new ValidacaoException(
                "Identificador inválido.",
                new Dictionary<string, string[]> { ["id"] = ["O id da categoria é obrigatório."] });
        }

        return await _categorias.ObterPorIdAsync(id, cancellationToken)
               ?? throw new RecursoNaoEncontradoException(NomeDoRecurso, id);
    }

    private async Task GarantirNomeUnicoAsync(string nome, Guid? idAtual, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            return; // a entidade já reporta o campo obrigatório.
        }

        var existente = await _categorias.ObterPorNomeAsync(nome, cancellationToken);
        if (existente is not null && existente.Id != idAtual)
        {
            throw new RecursoDuplicadoException(NomeDoRecurso.ToLowerInvariant(), "nome", nome);
        }
    }

    private static void GarantirCorpo(object? dados)
    {
        if (dados is null)
        {
            throw new ValidacaoException("O corpo da requisição é obrigatório.");
        }
    }

    private static CategoriaDto Mapear(Categoria categoria, int totalLivros) => new(
        categoria.Id,
        categoria.Nome,
        categoria.Descricao,
        categoria.Ativa,
        categoria.CriadaEm,
        totalLivros);
}
