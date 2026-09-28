namespace Biblioteca.Dominio.Excecoes;

/// <summary>Violação de unicidade. REST: 409. gRPC: AlreadyExists.</summary>
public sealed class RecursoDuplicadoException : DominioException
{
    public const string CodigoPadrao = "DUPLICADO";

    public RecursoDuplicadoException(string recurso, string campo, object valor)
        : base(CodigoPadrao, $"Já existe {recurso} com {campo} '{valor}'.")
    {
        Recurso = recurso;
        Campo = campo;
        Valor = valor;
    }

    public string Recurso { get; }

    public string Campo { get; }

    public object Valor { get; }
}
