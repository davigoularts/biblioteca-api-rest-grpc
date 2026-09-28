using System.Collections.Concurrent;
using Biblioteca.Dominio.Entidades;

namespace Biblioteca.Repositorio.Memoria;

/// <summary>
/// Armazenamento em memória compartilhado pelos repositórios (registrado como singleton).
/// Trocar por EF Core/Dapper significa escrever outra implementação das interfaces do domínio —
/// nada muda no domínio nem na apresentação.
/// </summary>
public sealed class BancoEmMemoria
{
    public ConcurrentDictionary<Guid, Categoria> Categorias { get; } = new();

    public ConcurrentDictionary<Guid, Livro> Livros { get; } = new();
}
