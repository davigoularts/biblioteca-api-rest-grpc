namespace Biblioteca.Dominio.Excecoes;

/// <summary>
/// O estado atual do sistema impede a operação (ex.: categoria inativa recebendo livro).
/// REST: 409. gRPC: FailedPrecondition.
/// </summary>
public sealed class RegraDeNegocioException : DominioException
{
    public const string CodigoPadrao = "REGRA_DE_NEGOCIO";

    public RegraDeNegocioException(string mensagem, string? regra = null)
        : base(CodigoPadrao, mensagem)
    {
        Regra = regra;
    }

    /// <summary>Identificador da regra violada (ex.: <c>LIVRO_CATEGORIA_INATIVA</c>).</summary>
    public string? Regra { get; }
}
