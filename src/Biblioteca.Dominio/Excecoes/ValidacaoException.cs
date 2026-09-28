namespace Biblioteca.Dominio.Excecoes;

/// <summary>Dados de entrada inválidos. REST: 400. gRPC: InvalidArgument.</summary>
public sealed class ValidacaoException : DominioException
{
    public const string CodigoPadrao = "VALIDACAO";

    public ValidacaoException(string mensagem, IReadOnlyDictionary<string, string[]>? detalhes = null)
        : base(CodigoPadrao, mensagem, detalhes)
    {
    }
}
