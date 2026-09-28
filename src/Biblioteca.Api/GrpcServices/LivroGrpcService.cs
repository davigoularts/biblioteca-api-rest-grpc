using Biblioteca.Api.Protos;
using Biblioteca.Dominio.Contratos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ContratoLivro = Biblioteca.Api.Protos.LivroService;
using IServicoDeLivro = Biblioteca.Dominio.Servicos.ILivroService;

namespace Biblioteca.Api.GrpcServices;

/// <summary>
/// Apresentação gRPC de Livro. Depende da MESMA <c>ILivroService</c> que o
/// <c>LivrosController</c> REST: a regra "categoria precisa estar ativa" não é
/// reescrita aqui, apenas propagada como FailedPrecondition pelo interceptor.
/// </summary>
public sealed class LivroGrpcService : ContratoLivro.LivroServiceBase
{
    private readonly IServicoDeLivro _livros;

    public LivroGrpcService(IServicoDeLivro livros) => _livros = livros;

    public override async Task<ListarLivrosResponse> Listar(
        ListarLivrosRequest request,
        ServerCallContext context)
    {
        var filtro = new FiltroLivroDto(
            MapeadorGrpc.ParaGuidOpcional(request.CategoriaId, "categoriaId"),
            MapeadorGrpc.ParaTextoOpcional(request.Termo));

        var livros = await _livros.ListarAsync(filtro, context.CancellationToken);

        var resposta = new ListarLivrosResponse();
        resposta.Livros.AddRange(livros.Select(MapeadorGrpc.ParaMensagem));

        return resposta;
    }

    public override async Task<LivroMessage> ObterPorId(LivroIdRequest request, ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        return MapeadorGrpc.ParaMensagem(await _livros.ObterPorIdAsync(id, context.CancellationToken));
    }

    public override async Task<LivroMessage> Criar(CriarLivroRequest request, ServerCallContext context)
    {
        var dados = new CriarLivroDto(
            request.Titulo,
            request.Autor,
            request.Isbn,
            request.AnoPublicacao,
            MapeadorGrpc.ParaGuid(request.CategoriaId, "categoriaId"),
            request.TotalExemplares);

        return MapeadorGrpc.ParaMensagem(await _livros.CriarAsync(dados, context.CancellationToken));
    }

    public override async Task<LivroMessage> Atualizar(AtualizarLivroRequest request, ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");

        var dados = new AtualizarLivroDto(
            request.Titulo,
            request.Autor,
            request.Isbn,
            request.AnoPublicacao,
            MapeadorGrpc.ParaGuid(request.CategoriaId, "categoriaId"),
            request.TotalExemplares);

        return MapeadorGrpc.ParaMensagem(await _livros.AtualizarAsync(id, dados, context.CancellationToken));
    }

    public override async Task<Empty> Remover(LivroIdRequest request, ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        await _livros.RemoverAsync(id, context.CancellationToken);

        return new Empty();
    }

    public override async Task<LivroMessage> Emprestar(LivroIdRequest request, ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        return MapeadorGrpc.ParaMensagem(await _livros.EmprestarAsync(id, context.CancellationToken));
    }

    public override async Task<LivroMessage> Devolver(LivroIdRequest request, ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        return MapeadorGrpc.ParaMensagem(await _livros.DevolverAsync(id, context.CancellationToken));
    }
}
