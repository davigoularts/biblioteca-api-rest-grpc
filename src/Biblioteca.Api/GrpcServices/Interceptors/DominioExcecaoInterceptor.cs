using Biblioteca.Dominio.Excecoes;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Biblioteca.Api.GrpcServices.Interceptors;

/// <summary>
/// Espelho gRPC do <c>TratamentoDeExcecoesMiddleware</c>: traduz a mesma exceção de
/// domínio para o <see cref="StatusCode"/> correspondente. As implementações gRPC não
/// contêm nenhum <c>try/catch</c> nem decisão de status.
/// </summary>
public sealed class DominioExcecaoInterceptor : Interceptor
{
    private readonly ILogger<DominioExcecaoInterceptor> _logger;

    public DominioExcecaoInterceptor(ILogger<DominioExcecaoInterceptor> logger) => _logger = logger;

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (DominioException excecao)
        {
            var status = MapearStatus(excecao);

            _logger.LogInformation(
                "Regra de domínio rejeitou {Metodo}: [{Codigo}] {Mensagem}",
                context.Method,
                excecao.Codigo,
                excecao.Message);

            throw new RpcException(
                new Status(status, MontarMensagem(excecao)),
                MontarTrailers(excecao));
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception excecao)
        {
            _logger.LogError(excecao, "Falha não tratada em {Metodo}", context.Method);

            throw new RpcException(new Status(
                StatusCode.Internal,
                "Ocorreu um erro inesperado ao processar a chamada."));
        }
    }

    /// <summary>
    /// Mapa exceção de domínio -> StatusCode gRPC, equivalente ao mapa HTTP do middleware.
    /// </summary>
    public static StatusCode MapearStatus(DominioException excecao) => excecao switch
    {
        ValidacaoException => StatusCode.InvalidArgument,
        RecursoNaoEncontradoException => StatusCode.NotFound,
        RecursoDuplicadoException => StatusCode.AlreadyExists,
        RegraDeNegocioException => StatusCode.FailedPrecondition,
        _ => StatusCode.Unknown,
    };

    /// <summary>
    /// Os erros por campo entram na mensagem de status — que o gRPC transporta
    /// percent-encoded e portanto aceita acentos — e não nos trailers.
    /// </summary>
    public static string MontarMensagem(DominioException excecao)
    {
        if (excecao.Detalhes.Count == 0)
        {
            return excecao.Message;
        }

        var campos = string.Join(
            " | ",
            excecao.Detalhes.Select(par => $"{par.Key}: {string.Join(" ", par.Value)}"));

        return $"{excecao.Message} [{campos}]";
    }

    /// <summary>
    /// Trailers viram cabeçalhos HTTP/2, que aceitam apenas ASCII imprimível.
    /// Por isso levam somente códigos e nomes de campo — nunca texto livre.
    /// </summary>
    public static Metadata MontarTrailers(DominioException excecao)
    {
        var trailers = new Metadata { { "codigo", ParaAscii(excecao.Codigo) } };

        if (excecao is RegraDeNegocioException { Regra: { } regra })
        {
            trailers.Add("regra", ParaAscii(regra));
        }

        if (excecao.Detalhes.Count > 0)
        {
            trailers.Add("campos-invalidos", ParaAscii(string.Join(",", excecao.Detalhes.Keys)));
        }

        return trailers;
    }

    private static string ParaAscii(string valor)
        => new(valor.Select(caractere => caractere is >= ' ' and <= '~' ? caractere : '?').ToArray());
}
