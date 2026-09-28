using System.Diagnostics;
using Biblioteca.Dominio.Excecoes;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Api.Rest.Middleware;

/// <summary>
/// Tradutor único de exceções de domínio para status HTTP.
/// Nenhum controller decide status code: eles apenas chamam o serviço e devolvem o resultado.
/// </summary>
public sealed class TratamentoDeExcecoesMiddleware
{
    private readonly RequestDelegate _proximo;
    private readonly ILogger<TratamentoDeExcecoesMiddleware> _logger;

    public TratamentoDeExcecoesMiddleware(RequestDelegate proximo, ILogger<TratamentoDeExcecoesMiddleware> logger)
    {
        _proximo = proximo;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            await _proximo(contexto);
        }
        catch (DominioException excecao)
        {
            var status = MapearStatus(excecao);

            _logger.LogInformation(
                "Regra de domínio rejeitou {Metodo} {Caminho}: [{Codigo}] {Mensagem}",
                contexto.Request.Method,
                contexto.Request.Path,
                excecao.Codigo,
                excecao.Message);

            await EscreverRespostaAsync(contexto, status, TituloPara(status), excecao.Message, excecao);
        }
        catch (OperationCanceledException) when (contexto.RequestAborted.IsCancellationRequested)
        {
            // Cliente desistiu da requisição: 499 (convenção do nginx), sem corpo.
            if (!contexto.Response.HasStarted)
            {
                contexto.Response.StatusCode = 499;
            }
        }
        catch (Exception excecao)
        {
            _logger.LogError(
                excecao,
                "Falha não tratada em {Metodo} {Caminho}",
                contexto.Request.Method,
                contexto.Request.Path);

            await EscreverRespostaAsync(
                contexto,
                StatusCodes.Status500InternalServerError,
                "Erro interno",
                "Ocorreu um erro inesperado ao processar a requisição.",
                excecaoDeDominio: null);
        }
    }

    /// <summary>
    /// Mapa exceção de domínio -> status HTTP. O espelho gRPC vive no
    /// <c>DominioExcecaoInterceptor</c>.
    /// </summary>
    public static int MapearStatus(DominioException excecao) => excecao switch
    {
        ValidacaoException => StatusCodes.Status400BadRequest,
        RecursoNaoEncontradoException => StatusCodes.Status404NotFound,
        RecursoDuplicadoException => StatusCodes.Status409Conflict,
        RegraDeNegocioException => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };

    private static string TituloPara(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Requisição inválida",
        StatusCodes.Status404NotFound => "Recurso não encontrado",
        StatusCodes.Status409Conflict => "Conflito com o estado atual",
        _ => "Erro",
    };

    private static async Task EscreverRespostaAsync(
        HttpContext contexto,
        int status,
        string titulo,
        string detalhe,
        DominioException? excecaoDeDominio)
    {
        if (contexto.Response.HasStarted)
        {
            return;
        }

        var problema = new ProblemDetails
        {
            Type = $"https://httpstatuses.io/{status}",
            Title = titulo,
            Status = status,
            Detail = detalhe,
            Instance = contexto.Request.Path,
        };

        problema.Extensions["traceId"] = Activity.Current?.Id ?? contexto.TraceIdentifier;

        if (excecaoDeDominio is not null)
        {
            problema.Extensions["codigo"] = excecaoDeDominio.Codigo;

            if (excecaoDeDominio.Detalhes.Count > 0)
            {
                problema.Extensions["erros"] = excecaoDeDominio.Detalhes;
            }

            if (excecaoDeDominio is RegraDeNegocioException { Regra: { } regra })
            {
                problema.Extensions["regra"] = regra;
            }
        }

        contexto.Response.Clear();
        contexto.Response.StatusCode = status;

        // O contentType precisa ir no overload: WriteAsJsonAsync(valor, ct) sobrescreveria
        // o Content-Type com "application/json".
        await contexto.Response.WriteAsJsonAsync(
            problema,
            options: null,
            contentType: "application/problem+json",
            contexto.RequestAborted);
    }
}
