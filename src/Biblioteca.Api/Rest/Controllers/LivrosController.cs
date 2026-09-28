using Biblioteca.Dominio.Contratos;
using Biblioteca.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Api.Rest.Controllers;

/// <summary>
/// Apresentação REST de Livro. Consome exatamente a mesma <see cref="ILivroService"/>
/// usada pelo serviço gRPC.
/// </summary>
[ApiController]
[Route("api/livros")]
[Produces("application/json")]
public sealed class LivrosController : ControllerBase
{
    private readonly ILivroService _livros;

    public LivrosController(ILivroService livros) => _livros = livros;

    /// <summary>Lista livros, opcionalmente filtrando por categoria e/ou termo de busca.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LivroDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LivroDto>>> Listar(
        [FromQuery] Guid? categoriaId,
        [FromQuery] string? termo,
        CancellationToken cancellationToken)
        => Ok(await _livros.ListarAsync(new FiltroLivroDto(categoriaId, termo), cancellationToken));

    /// <summary>Obtém um livro pelo identificador.</summary>
    [HttpGet("{id:guid}", Name = nameof(ObterLivroPorId))]
    [ProducesResponseType(typeof(LivroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LivroDto>> ObterLivroPorId(Guid id, CancellationToken cancellationToken)
        => Ok(await _livros.ObterPorIdAsync(id, cancellationToken));

    /// <summary>
    /// Cadastra um livro. Retorna 404 se a categoria não existir, 409 se ela estiver
    /// inativa (regra R1) ou se o ISBN já estiver em uso (regra R4).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(LivroDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LivroDto>> Criar(
        [FromBody] CriarLivroDto? dados,
        CancellationToken cancellationToken)
    {
        var criado = await _livros.CriarAsync(dados!, cancellationToken);
        return CreatedAtRoute(nameof(ObterLivroPorId), new { id = criado.Id }, criado);
    }

    /// <summary>Atualiza um livro. Ao trocar de categoria, a nova precisa estar ativa (regra R1).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(LivroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LivroDto>> Atualizar(
        Guid id,
        [FromBody] AtualizarLivroDto? dados,
        CancellationToken cancellationToken)
        => Ok(await _livros.AtualizarAsync(id, dados!, cancellationToken));

    /// <summary>Remove um livro. Retorna 409 se houver exemplares emprestados (regra R8).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await _livros.RemoverAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Empresta um exemplar. Retorna 409 se a categoria estiver inativa (regra R7)
    /// ou se não houver exemplar disponível (regra R6).
    /// </summary>
    [HttpPost("{id:guid}/emprestimos")]
    [ProducesResponseType(typeof(LivroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LivroDto>> Emprestar(Guid id, CancellationToken cancellationToken)
        => Ok(await _livros.EmprestarAsync(id, cancellationToken));

    /// <summary>Devolve um exemplar. Retorna 409 se não houver empréstimo em aberto.</summary>
    [HttpPost("{id:guid}/devolucoes")]
    [ProducesResponseType(typeof(LivroDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LivroDto>> Devolver(Guid id, CancellationToken cancellationToken)
        => Ok(await _livros.DevolverAsync(id, cancellationToken));
}
