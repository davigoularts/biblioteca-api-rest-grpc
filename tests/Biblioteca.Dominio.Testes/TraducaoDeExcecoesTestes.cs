using Biblioteca.Api.GrpcServices.Interceptors;
using Biblioteca.Api.Rest.Middleware;
using Biblioteca.Dominio.Excecoes;
using Grpc.Core;
using Xunit;

namespace Biblioteca.Dominio.Testes;

/// <summary>
/// Garante que as duas bordas traduzem a MESMA exceção de domínio para o status
/// correto do seu protocolo — e que nenhuma delas inventa regra própria.
/// </summary>
public sealed class TraducaoDeExcecoesTestes
{
    public static TheoryData<DominioException, int, StatusCode> Mapeamentos() => new()
    {
        { new ValidacaoException("campo inválido"), 400, StatusCode.InvalidArgument },
        { new RecursoNaoEncontradoException("Livro", Guid.Empty), 404, StatusCode.NotFound },
        { new RecursoDuplicadoException("livro", "ISBN", "123"), 409, StatusCode.AlreadyExists },
        { new RegraDeNegocioException("categoria inativa"), 409, StatusCode.FailedPrecondition },
    };

    [Theory]
    [MemberData(nameof(Mapeamentos))]
    public void Excecao_de_dominio_deve_virar_o_status_esperado_nos_dois_protocolos(
        DominioException excecao,
        int statusHttpEsperado,
        StatusCode statusGrpcEsperado)
    {
        Assert.Equal(statusHttpEsperado, TratamentoDeExcecoesMiddleware.MapearStatus(excecao));
        Assert.Equal(statusGrpcEsperado, DominioExcecaoInterceptor.MapearStatus(excecao));
    }

    /// <summary>
    /// Trailers gRPC viram cabeçalhos HTTP/2: qualquer acento derruba a resposta com 500.
    /// </summary>
    [Fact]
    public void Trailers_gRPC_devem_conter_apenas_ascii_imprimivel()
    {
        var excecao = new ValidacaoException(
            "Dados inválidos para o livro.",
            new Dictionary<string, string[]>
            {
                ["titulo"] = ["O título é obrigatório."],
                ["anoPublicacao"] = ["O ano de publicação é inválido."],
            });

        var trailers = DominioExcecaoInterceptor.MontarTrailers(excecao);

        Assert.All(trailers, entrada => Assert.All(
            entrada.Value,
            caractere => Assert.InRange(caractere, ' ', '~')));

        Assert.Equal("VALIDACAO", trailers.GetValue("codigo"));
        Assert.Equal("titulo,anoPublicacao", trailers.GetValue("campos-invalidos"));
    }

    [Fact]
    public void Mensagem_gRPC_deve_levar_os_erros_por_campo_com_acentuacao_preservada()
    {
        var excecao = new ValidacaoException(
            "Dados inválidos para o livro.",
            new Dictionary<string, string[]> { ["titulo"] = ["O título é obrigatório."] });

        var mensagem = DominioExcecaoInterceptor.MontarMensagem(excecao);

        Assert.Equal("Dados inválidos para o livro. [titulo: O título é obrigatório.]", mensagem);
    }

    [Fact]
    public void Mensagem_gRPC_sem_erros_por_campo_deve_ser_a_propria_mensagem()
    {
        var excecao = new RegraDeNegocioException("Categoria inativa.", "LIVRO_CATEGORIA_INATIVA");

        Assert.Equal("Categoria inativa.", DominioExcecaoInterceptor.MontarMensagem(excecao));
    }
}
