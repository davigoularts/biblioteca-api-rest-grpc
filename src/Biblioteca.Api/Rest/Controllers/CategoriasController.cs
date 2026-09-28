using Biblioteca.Dominio.Contratos;
using Biblioteca.Dominio.Servicos;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Api.Rest.Controllers;

/// <summary>
/// Apresentação REST de Categoria. Só traduz HTTP -> caso de uso; nenhuma decisão de
/// negócio nem de status code aqui (o middleware cuida dos erros).
/// </summary>
[ApiController]
[Route("api/categorias")]
[Produces("application/json")]
public sealed class CategoriasController : ControllerBase
{
    private readonly ICategoriaService _categorias;

    public CategoriasController(ICategoriaService categorias) => _categorias = categorias;

    /// <summary>Lista as categorias cadastradas.</summary>
    /// <param name="apenasAtivas">Quando <c>true</c>, devolve somente categorias ativas.</param>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoriaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoriaDto>>> Listar(
        [FromQuery] bool apenasAtivas = false,
        CancellationToken cancellationToken = default)
        => Ok(await _categorias.ListarAsync(apenasAtivas, cancellationToken));

    /// <summary>Obtém uma categoria pelo identificador.</summary>
    [HttpGet("{id:guid}", Name = nameof(ObterCategoriaPorId))]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaDto>> ObterCategoriaPorId(
        Guid id,
        CancellationToken cancellationToken)
        => Ok(await _categorias.ObterPorIdAsync(id, cancellationToken));

    /// <summary>Cadastra uma categoria.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaDto>> Criar(
        [FromBody] CriarCategoriaDto? dados,
        CancellationToken cancellationToken)
    {
        var criada = await _categorias.CriarAsync(dados!, cancellationToken);
        return CreatedAtRoute(nameof(ObterCategoriaPorId), new { id = criada.Id }, criada);
    }

    /// <summary>Atualiza nome e descrição de uma categoria.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaDto>> Atualizar(
        Guid id,
        [FromBody] AtualizarCategoriaDto? dados,
        CancellationToken cancellationToken)
        => Ok(await _categorias.AtualizarAsync(id, dados!, cancellationToken));

    /// <summary>Ativa a categoria, liberando vínculo de livros e empréstimos.</summary>
    [HttpPatch("{id:guid}/ativar")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoriaDto>> Ativar(Guid id, CancellationToken cancellationToken)
        => Ok(await _categorias.AtivarAsync(id, cancellationToken));

    /// <summary>
    /// Inativa a categoria. Retorna 409 quando existe livro com exemplar emprestado (regra R2).
    /// </summary>
    [HttpPatch("{id:guid}/inativar")]
    [ProducesResponseType(typeof(CategoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoriaDto>> Inativar(Guid id, CancellationToken cancellationToken)
        => Ok(await _categorias.InativarAsync(id, cancellationToken));

    /// <summary>
    /// Remove a categoria. Retorna 409 quando há livros vinculados (regra R3).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(Guid id, CancellationToken cancellationToken)
    {
        await _categorias.RemoverAsync(id, cancellationToken);
        return NoContent();
    }
}
