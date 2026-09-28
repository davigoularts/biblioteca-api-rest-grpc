using Biblioteca.Dominio.Contratos;
using Biblioteca.Dominio.Excecoes;
using Biblioteca.Repositorio.Memoria;
using Xunit;

namespace Biblioteca.Dominio.Testes;

public sealed class LivroServiceTestes
{
    private static CriarLivroDto NovoLivro(Guid categoriaId, string isbn = "9781234567897") => new(
        Titulo: "Refatoração",
        Autor: "Martin Fowler",
        Isbn: isbn,
        AnoPublicacao: 2018,
        CategoriaId: categoriaId,
        TotalExemplares: 2);

    [Fact]
    public async Task Criar_em_categoria_ativa_deve_cadastrar_o_livro()
    {
        var cenario = Cenario.Criar();

        var livro = await cenario.Livros.CriarAsync(NovoLivro(SemeadorDeDados.CategoriaTecnologiaId));

        Assert.NotEqual(Guid.Empty, livro.Id);
        Assert.Equal("Tecnologia", livro.CategoriaNome);
        Assert.Equal(2, livro.ExemplaresDisponiveis);
    }

    // R1 - a regra central do trabalho.
    [Fact]
    public async Task Criar_em_categoria_inativa_deve_violar_regra_de_negocio()
    {
        var cenario = Cenario.Criar();

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => cenario.Livros.CriarAsync(NovoLivro(SemeadorDeDados.CategoriaInativaId)));

        Assert.Equal("LIVRO_CATEGORIA_INATIVA", excecao.Regra);
    }

    [Fact]
    public async Task Criar_em_categoria_inexistente_deve_retornar_nao_encontrado()
    {
        var cenario = Cenario.Criar();

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => cenario.Livros.CriarAsync(NovoLivro(Guid.NewGuid())));
    }

    // R4
    [Fact]
    public async Task Criar_com_isbn_existente_deve_ser_duplicado()
    {
        var cenario = Cenario.Criar();

        await Assert.ThrowsAsync<RecursoDuplicadoException>(
            () => cenario.Livros.CriarAsync(
                NovoLivro(SemeadorDeDados.CategoriaTecnologiaId, "978-0-13-449416-6")));
    }

    [Fact]
    public async Task Criar_sem_titulo_deve_falhar_na_validacao_com_erro_por_campo()
    {
        var cenario = Cenario.Criar();
        var dados = NovoLivro(SemeadorDeDados.CategoriaTecnologiaId) with { Titulo = "  " };

        var excecao = await Assert.ThrowsAsync<ValidacaoException>(() => cenario.Livros.CriarAsync(dados));

        Assert.True(excecao.Detalhes.ContainsKey("titulo"));
    }

    // R1 na atualização: mover livro para categoria inativa é proibido.
    [Fact]
    public async Task Atualizar_movendo_para_categoria_inativa_deve_violar_regra_de_negocio()
    {
        var cenario = Cenario.Criar();
        var atual = await cenario.Livros.ObterPorIdAsync(SemeadorDeDados.LivroCleanArchitectureId);

        var dados = new AtualizarLivroDto(
            atual.Titulo,
            atual.Autor,
            atual.Isbn,
            atual.AnoPublicacao,
            SemeadorDeDados.CategoriaInativaId,
            atual.TotalExemplares);

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => cenario.Livros.AtualizarAsync(atual.Id, dados));
    }

    [Fact]
    public async Task Atualizar_sem_trocar_categoria_deve_funcionar_mesmo_com_categoria_inativa()
    {
        var cenario = Cenario.Criar();

        // Cadastra numa categoria ativa e depois inativa a categoria.
        var livro = await cenario.Livros.CriarAsync(NovoLivro(SemeadorDeDados.CategoriaTecnologiaId));
        await cenario.Categorias.InativarAsync(SemeadorDeDados.CategoriaTecnologiaId);

        var atualizado = await cenario.Livros.AtualizarAsync(
            livro.Id,
            new AtualizarLivroDto(
                "Refatoração (2ª edição)",
                livro.Autor,
                livro.Isbn,
                livro.AnoPublicacao,
                livro.CategoriaId,
                livro.TotalExemplares));

        Assert.Equal("Refatoração (2ª edição)", atualizado.Titulo);
        Assert.False(atualizado.CategoriaAtiva);
    }

    // R6
    [Fact]
    public async Task Emprestar_sem_exemplar_disponivel_deve_violar_regra_de_negocio()
    {
        var cenario = Cenario.Criar();

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => cenario.Livros.EmprestarAsync(SemeadorDeDados.LivroGrandeSertaoId));

        Assert.Equal("LIVRO_SEM_EXEMPLAR_DISPONIVEL", excecao.Regra);
    }

    // R7 - outra regra entre agregados.
    [Fact]
    public async Task Emprestar_livro_de_categoria_inativa_deve_violar_regra_de_negocio()
    {
        var cenario = Cenario.Criar();
        await cenario.Categorias.InativarAsync(SemeadorDeDados.CategoriaTecnologiaId);

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => cenario.Livros.EmprestarAsync(SemeadorDeDados.LivroCleanArchitectureId));

        Assert.Equal("LIVRO_CATEGORIA_INATIVA", excecao.Regra);
    }

    [Fact]
    public async Task Emprestar_e_devolver_deve_reequilibrar_os_exemplares()
    {
        var cenario = Cenario.Criar();

        var emprestado = await cenario.Livros.EmprestarAsync(SemeadorDeDados.LivroDddId);
        Assert.Equal(1, emprestado.ExemplaresEmprestados);
        Assert.Equal(1, emprestado.ExemplaresDisponiveis);

        var devolvido = await cenario.Livros.DevolverAsync(SemeadorDeDados.LivroDddId);
        Assert.Equal(0, devolvido.ExemplaresEmprestados);
        Assert.Equal(2, devolvido.ExemplaresDisponiveis);
    }

    [Fact]
    public async Task Devolver_sem_emprestimo_ativo_deve_violar_regra_de_negocio()
    {
        var cenario = Cenario.Criar();

        await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => cenario.Livros.DevolverAsync(SemeadorDeDados.LivroDddId));
    }

    // R8
    [Fact]
    public async Task Remover_livro_com_emprestimo_ativo_deve_violar_regra_de_negocio()
    {
        var cenario = Cenario.Criar();

        var excecao = await Assert.ThrowsAsync<RegraDeNegocioException>(
            () => cenario.Livros.RemoverAsync(SemeadorDeDados.LivroGrandeSertaoId));

        Assert.Equal("LIVRO_COM_EMPRESTIMO_ATIVO", excecao.Regra);
    }

    [Fact]
    public async Task Obter_livro_inexistente_deve_retornar_nao_encontrado()
    {
        var cenario = Cenario.Criar();

        await Assert.ThrowsAsync<RecursoNaoEncontradoException>(
            () => cenario.Livros.ObterPorIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Listar_deve_aceitar_filtro_por_categoria_e_por_termo()
    {
        var cenario = Cenario.Criar();

        var porCategoria = await cenario.Livros.ListarAsync(
            new FiltroLivroDto(CategoriaId: SemeadorDeDados.CategoriaTecnologiaId));

        var porTermo = await cenario.Livros.ListarAsync(new FiltroLivroDto(Termo: "machado"));

        Assert.Equal(2, porCategoria.Count);
        Assert.Single(porTermo);
        Assert.Equal("Dom Casmurro", porTermo[0].Titulo);
    }
}
