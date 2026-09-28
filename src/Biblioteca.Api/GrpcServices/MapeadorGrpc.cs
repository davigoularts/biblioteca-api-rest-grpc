using Biblioteca.Api.Protos;
using Biblioteca.Dominio.Contratos;
using Biblioteca.Dominio.Excecoes;
using Google.Protobuf.WellKnownTypes;

namespace Biblioteca.Api.GrpcServices;

/// <summary>
/// Conversão mensagem protobuf &lt;-&gt; DTO de domínio. É só tradução de formato:
/// as regras continuam exclusivamente nos serviços de domínio.
/// </summary>
internal static class MapeadorGrpc
{
    public static Guid ParaGuid(string? valor, string campo)
    {
        if (Guid.TryParse(valor, out var id) && id != Guid.Empty)
        {
            return id;
        }

        throw new ValidacaoException(
            "Identificador inválido.",
            new Dictionary<string, string[]>
            {
                [campo] = [string.IsNullOrWhiteSpace(valor)
                    ? "O identificador é obrigatório."
                    : $"'{valor}' não é um GUID válido."],
            });
    }

    /// <summary>Campos de filtro vazios em proto3 significam "sem filtro".</summary>
    public static Guid? ParaGuidOpcional(string? valor, string campo)
        => string.IsNullOrWhiteSpace(valor) ? null : ParaGuid(valor, campo);

    public static string? ParaTextoOpcional(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor;

    public static CategoriaMessage ParaMensagem(CategoriaDto dto) => new()
    {
        Id = dto.Id.ToString(),
        Nome = dto.Nome,
        Descricao = dto.Descricao,
        Ativa = dto.Ativa,
        CriadaEm = ParaTimestamp(dto.CriadaEm),
        TotalLivros = dto.TotalLivros,
    };

    public static LivroMessage ParaMensagem(LivroDto dto) => new()
    {
        Id = dto.Id.ToString(),
        Titulo = dto.Titulo,
        Autor = dto.Autor,
        Isbn = dto.Isbn,
        AnoPublicacao = dto.AnoPublicacao,
        CategoriaId = dto.CategoriaId.ToString(),
        CategoriaNome = dto.CategoriaNome,
        CategoriaAtiva = dto.CategoriaAtiva,
        TotalExemplares = dto.TotalExemplares,
        ExemplaresEmprestados = dto.ExemplaresEmprestados,
        ExemplaresDisponiveis = dto.ExemplaresDisponiveis,
        CadastradoEm = ParaTimestamp(dto.CadastradoEm),
    };

    private static Timestamp ParaTimestamp(DateTime valor)
    {
        var utc = valor.Kind switch
        {
            DateTimeKind.Utc => valor,
            DateTimeKind.Local => valor.ToUniversalTime(),
            _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc),
        };

        return Timestamp.FromDateTime(utc);
    }
}
