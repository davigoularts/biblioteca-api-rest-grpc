using Biblioteca.Api.Protos;
using Biblioteca.Dominio.Contratos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using ContratoCategoria = Biblioteca.Api.Protos.CategoriaService;
using IServicoDeCategoria = Biblioteca.Dominio.Servicos.ICategoriaService;

namespace Biblioteca.Api.GrpcServices;

/// <summary>
/// Apresentação gRPC de Categoria. Depende da MESMA <c>ICategoriaService</c> que o
/// <c>CategoriasController</c> REST — é esse reaproveitamento que garante uma única
/// cópia da regra de negócio.
/// </summary>
public sealed class CategoriaGrpcService : ContratoCategoria.CategoriaServiceBase
{
    private readonly IServicoDeCategoria _categorias;

    public CategoriaGrpcService(IServicoDeCategoria categorias) => _categorias = categorias;

    public override async Task<ListarCategoriasResponse> Listar(
        ListarCategoriasRequest request,
        ServerCallContext context)
    {
        var categorias = await _categorias.ListarAsync(request.ApenasAtivas, context.CancellationToken);

        var resposta = new ListarCategoriasResponse();
        resposta.Categorias.AddRange(categorias.Select(MapeadorGrpc.ParaMensagem));

        return resposta;
    }

    public override async Task<CategoriaMessage> ObterPorId(
        CategoriaIdRequest request,
        ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        return MapeadorGrpc.ParaMensagem(await _categorias.ObterPorIdAsync(id, context.CancellationToken));
    }

    public override async Task<CategoriaMessage> Criar(
        CriarCategoriaRequest request,
        ServerCallContext context)
    {
        var dados = new CriarCategoriaDto(request.Nome, request.Descricao, request.Ativa);
        return MapeadorGrpc.ParaMensagem(await _categorias.CriarAsync(dados, context.CancellationToken));
    }

    public override async Task<CategoriaMessage> Atualizar(
        AtualizarCategoriaRequest request,
        ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        var dados = new AtualizarCategoriaDto(request.Nome, request.Descricao);

        return MapeadorGrpc.ParaMensagem(await _categorias.AtualizarAsync(id, dados, context.CancellationToken));
    }

    public override async Task<CategoriaMessage> Ativar(CategoriaIdRequest request, ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        return MapeadorGrpc.ParaMensagem(await _categorias.AtivarAsync(id, context.CancellationToken));
    }

    public override async Task<CategoriaMessage> Inativar(CategoriaIdRequest request, ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        return MapeadorGrpc.ParaMensagem(await _categorias.InativarAsync(id, context.CancellationToken));
    }

    public override async Task<Empty> Remover(CategoriaIdRequest request, ServerCallContext context)
    {
        var id = MapeadorGrpc.ParaGuid(request.Id, "id");
        await _categorias.RemoverAsync(id, context.CancellationToken);

        return new Empty();
    }
}
