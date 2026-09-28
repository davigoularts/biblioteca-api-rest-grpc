namespace Biblioteca.Dominio.Excecoes;

/// <summary>Agregado inexistente. REST: 404. gRPC: NotFound.</summary>
public sealed class RecursoNaoEncontradoException : DominioException
{
    public const string CodigoPadrao = "NAO_ENCONTRADO";

    public RecursoNaoEncontradoException(string recurso, object chave)
        : base(CodigoPadrao, $"{recurso} '{chave}' não foi encontrado(a).")
    {
        Recurso = recurso;
        Chave = chave;
    }

    public string Recurso { get; }

    public object Chave { get; }
}
