using Biblioteca.Dominio.Contratos;
using Biblioteca.Dominio.Excecoes;
using Biblioteca.Repositorio.Memoria;
using Xunit;

namespace Biblioteca.Dominio.Testes;

public sealed class CategoriaServiceTestes
{
    [Fact]
    public async Task Listar_deve_poder_filtrar_somente_ativas_e_contar_livros()
    {
        var cenario = Cenario.Criar();

        var todas = await cenario.Categorias.ListarAsync();
        var ativas = await cenario.Categorias.ListarAsync(apenasAtivas: true);

        Assert.Equal(3, todas.Count);
        Assert.Equal(2, ativas.Count);
        Assert.Equal(2, todas.Single(c => c.Id == SemeadorDeDados.CategoriaTecnologiaId).TotalLivros);
    }

    [Fact]
    public async Task Criar_com_nome_repetido_deve_ser_duplicado()
    {
        var cenario = Cenario.Criar();

        await Assert.ThrowsAsync<RecursoDuplicadoException>(
            () => cenario.Categorias.CriarAsync(new CriarCategoriaDto("  tecnologia ", "duplicada")));
    }

    [Fact]
    public async Task Criar_sem_nome_deve_falhar_na_validacao()
    {
        var cenario = Cenario.Criar();

        var excecao = await Assert.ThrowsAsync<ValidacaoException>(
            () => cenario.Categorias.CriarAsync(new CriarCategoriaDto(null, null)));

        Assert.True(excecao.Detalhes.ContainsKey("nome"));
    }

    // R2
    [Fact]
    public async Task Inativar_categoria_com_emprestimo_ativo_deve_violar_regra_de_negocio()
    {
        var cenario = Cenario.Criar();

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => cenario.Categorias.InativarAsync(SemeadorDeDados.CategoriaLiteraturaId));

        Assert.Equal("CATEGORIA_COM_EMPRESTIMO_ATIVO", excecao.Regra);
    }

    [Fact]
    public async Task Inativar_categoria_sem_emprestimo_ativo_deve_funcionar()
    {
        var cenario = Cenario.Criar();

        var categoria = await cenario.Categorias.InativarAsync(SemeadorDeDados.CategoriaTecnologiaId);

        Assert.False(categoria.Ativa);
    }

    // R3
    [Fact]
    public async Task Remover_categoria_com_livros_deve_violar_regra_de_negocio()
    {
        var cenario = Cenario.Criar();

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => cenario.Categorias.RemoverAsync(SemeadorDeDados.CategoriaTecnologiaId));

        Assert.Equal("CATEGORIA_COM_LIVROS_VINCULADOS", excecao.Regra);
    }

    [Fact]
    public async Task Remover_categoria_vazia_deve_funcionar()
    {
        var cenario = Cenario.Criar();
        var criada = await cenario.Categorias.CriarAsync(new CriarCategoriaDto("Quadrinhos", "HQs"));

        await cenario.Categorias.RemoverAsync(criada.Id);

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => cenario.Categorias.ObterPorIdAsync(criada.Id));
    }

    [Fact]
    public async Task Ativar_categoria_deve_liberar_o_vinculo_de_livros()
    {
        var cenario = Cenario.Criar();

        await cenario.Categorias.AtivarAsync(SemeadorDeDados.CategoriaInativaId);

        var livro = await cenario.Livros.CriarAsync(new CriarLivroDto(
            "Revista Byte",
            "Diversos",
            "0360528400",
            1990,
            SemeadorDeDados.CategoriaInativaId,
            1));

        Assert.Equal(SemeadorDeDados.CategoriaInativaId, livro.CategoriaId);
    }

    [Fact]
    public async Task Atualizar_categoria_inexistente_deve_retornar_nao_encontrado()
    {
        var cenario = Cenario.Criar();

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => cenario.Categorias.AtualizarAsync(
                Guid.NewGuid(),
                new AtualizarCategoriaDto("Qualquer", null)));
    }
}
