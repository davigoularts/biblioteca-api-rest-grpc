namespace Biblioteca.Dominio.Excecoes;

/// <summary>
/// Raiz de todas as falhas previstas pelo domínio.
/// A camada de apresentação (REST ou gRPC) captura esta exceção e a traduz para o
/// protocolo correspondente — o domínio nunca conhece status HTTP nem StatusCode gRPC.
/// </summary>
public abstract class DominioException : Exception
{
    protected DominioException(
        string codigo,
        string mensagem,
        IReadOnlyDictionary<string, string[]>? detalhes = null)
        : base(mensagem)
    {
        Codigo = codigo;
        Detalhes = detalhes ?? new Dictionary<string, string[]>();
    }

    /// <summary>Código estável da falha, útil para clientes automatizados.</summary>
    public string Codigo { get; }

    /// <summary>Erros por campo, quando aplicável.</summary>
    public IReadOnlyDictionary<string, string[]> Detalhes { get; }
}
