using Biblioteca.Dominio.Entidades;

namespace Biblioteca.Repositorio.Memoria;

/// <summary>
/// Carga inicial com identificadores fixos, para que a coleção do Postman e o README
/// possam apontar direto para os cenários de erro sem precisar descobrir ids.
/// </summary>
public static class SemeadorDeDados
{
    public static readonly Guid CategoriaTecnologiaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid CategoriaLiteraturaId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    /// <summary>Categoria propositalmente inativa: usada para demonstrar a regra R1.</summary>
    public static readonly Guid CategoriaInativaId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public static readonly Guid LivroCleanArchitectureId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid LivroDddId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    public static readonly Guid LivroDomCasmurroId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    /// <summary>Único exemplar e já emprestado: usado para demonstrar as regras R2 e R6.</summary>
    public static readonly Guid LivroGrandeSertaoId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    public static void Popular(BancoEmMemoria banco)
    {
        var criadaEm = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        Adicionar(banco, Categoria.Reidratar(
            CategoriaTecnologiaId,
            "Tecnologia",
            "Engenharia de software, arquitetura e infraestrutura.",
            ativa: true,
            criadaEm));

        Adicionar(banco, Categoria.Reidratar(
            CategoriaLiteraturaId,
            "Literatura Brasileira",
            "Clássicos e contemporâneos da literatura nacional.",
            ativa: true,
            criadaEm));

        Adicionar(banco, Categoria.Reidratar(
            CategoriaInativaId,
            "Periódicos Descontinuados",
            "Acervo desativado: não recebe novos títulos nem empréstimos.",
            ativa: false,
            criadaEm));

        Adicionar(banco, Livro.Reidratar(
            LivroCleanArchitectureId,
            "Clean Architecture",
            "Robert C. Martin",
            "9780134494166",
            2017,
            CategoriaTecnologiaId,
            totalExemplares: 4,
            exemplaresEmprestados: 0,
            criadaEm));

        Adicionar(banco, Livro.Reidratar(
            LivroDddId,
            "Domain-Driven Design",
            "Eric Evans",
            "9780321125217",
            2003,
            CategoriaTecnologiaId,
            totalExemplares: 2,
            exemplaresEmprestados: 0,
            criadaEm));

        Adicionar(banco, Livro.Reidratar(
            LivroDomCasmurroId,
            "Dom Casmurro",
            "Machado de Assis",
            "9788525406581",
            1899,
            CategoriaLiteraturaId,
            totalExemplares: 3,
            exemplaresEmprestados: 0,
            criadaEm));

        Adicionar(banco, Livro.Reidratar(
            LivroGrandeSertaoId,
            "Grande Sertão: Veredas",
            "João Guimarães Rosa",
            "9788520925577",
            1956,
            CategoriaLiteraturaId,
            totalExemplares: 1,
            exemplaresEmprestados: 1,
            criadaEm));
    }

    private static void Adicionar(BancoEmMemoria banco, Categoria categoria)
        => banco.Categorias[categoria.Id] = categoria;

    private static void Adicionar(BancoEmMemoria banco, Livro livro)
        => banco.Livros[livro.Id] = livro;
}
