using Biblioteca.Dominio.Excecoes;

namespace Biblioteca.Dominio.Entidades;

/// <summary>
/// Agregado Livro. Referencia a Categoria por identidade (<see cref="CategoriaId"/>);
/// a checagem de "categoria ativa" acontece no serviço de domínio, que enxerga os dois agregados.
/// </summary>
public sealed class Livro
{
    public const int TamanhoMaximoTitulo = 150;
    public const int TamanhoMaximoAutor = 120;
    public const int AnoMinimoPublicacao = 1400;
    public const int MaximoExemplares = 10_000;

    private Livro(
        Guid id,
        string titulo,
        string autor,
        string isbn,
        int anoPublicacao,
        Guid categoriaId,
        int totalExemplares,
        int exemplaresEmprestados,
        DateTime cadastradoEm)
    {
        Id = id;
        Titulo = titulo;
        Autor = autor;
        Isbn = isbn;
        AnoPublicacao = anoPublicacao;
        CategoriaId = categoriaId;
        TotalExemplares = totalExemplares;
        ExemplaresEmprestados = exemplaresEmprestados;
        CadastradoEm = cadastradoEm;
    }

    public Guid Id { get; }

    public string Titulo { get; private set; }

    public string Autor { get; private set; }

    public string Isbn { get; private set; }

    public int AnoPublicacao { get; private set; }

    public Guid CategoriaId { get; private set; }

    public int TotalExemplares { get; private set; }

    public int ExemplaresEmprestados { get; private set; }

    public DateTime CadastradoEm { get; }

    public int ExemplaresDisponiveis => TotalExemplares - ExemplaresEmprestados;

    public bool PossuiEmprestimoAtivo => ExemplaresEmprestados > 0;

    public static Livro Criar(
        string? titulo,
        string? autor,
        string? isbn,
        int anoPublicacao,
        Guid categoriaId,
        int totalExemplares)
    {
        var dados = Validar(titulo, autor, isbn, anoPublicacao, categoriaId, totalExemplares);

        return new Livro(
            Guid.NewGuid(),
            dados.Titulo,
            dados.Autor,
            dados.Isbn,
            anoPublicacao,
            categoriaId,
            totalExemplares,
            exemplaresEmprestados: 0,
            cadastradoEm: DateTime.UtcNow);
    }

    public static Livro Reidratar(
        Guid id,
        string titulo,
        string autor,
        string isbn,
        int anoPublicacao,
        Guid categoriaId,
        int totalExemplares,
        int exemplaresEmprestados,
        DateTime cadastradoEm)
        => new(id, titulo, autor, isbn, anoPublicacao, categoriaId, totalExemplares, exemplaresEmprestados, cadastradoEm);

    public void Atualizar(
        string? titulo,
        string? autor,
        string? isbn,
        int anoPublicacao,
        Guid categoriaId,
        int totalExemplares)
    {
        var dados = Validar(titulo, autor, isbn, anoPublicacao, categoriaId, totalExemplares);

        if (totalExemplares < ExemplaresEmprestados)
        {
            throw new RegraDeNegocioException(
                $"Não é possível definir {totalExemplares} exemplar(es): {ExemplaresEmprestados} já está(ão) emprestado(s).",
                "LIVRO_EXEMPLARES_MENOR_QUE_EMPRESTADOS");
        }

        Titulo = dados.Titulo;
        Autor = dados.Autor;
        Isbn = dados.Isbn;
        AnoPublicacao = anoPublicacao;
        CategoriaId = categoriaId;
        TotalExemplares = totalExemplares;
    }

    public void Emprestar()
    {
        if (ExemplaresDisponiveis <= 0)
        {
            throw new RegraDeNegocioException(
                $"O livro '{Titulo}' não possui exemplares disponíveis para empréstimo.",
                "LIVRO_SEM_EXEMPLAR_DISPONIVEL");
        }

        ExemplaresEmprestados++;
    }

    public void Devolver()
    {
        if (ExemplaresEmprestados <= 0)
        {
            throw new RegraDeNegocioException(
                $"O livro '{Titulo}' não possui exemplares emprestados.",
                "LIVRO_SEM_EMPRESTIMO_ATIVO");
        }

        ExemplaresEmprestados--;
    }

    /// <summary>
    /// Remove separadores e normaliza o ISBN. Exposto para que o serviço consiga
    /// checar unicidade antes de mutar a entidade.
    /// </summary>
    public static string NormalizarIsbn(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return string.Empty;
        }

        var limpo = new string(isbn.Where(char.IsLetterOrDigit).ToArray());
        return limpo.ToUpperInvariant();
    }

    private static (string Titulo, string Autor, string Isbn) Validar(
        string? titulo,
        string? autor,
        string? isbn,
        int anoPublicacao,
        Guid categoriaId,
        int totalExemplares)
    {
        var tituloNormalizado = (titulo ?? string.Empty).Trim();
        var autorNormalizado = (autor ?? string.Empty).Trim();
        var isbnNormalizado = NormalizarIsbn(isbn);
        var anoMaximo = DateTime.UtcNow.Year + 1;

        new ColetorDeErros()
            .AdicionarSe(tituloNormalizado.Length == 0, "titulo", "O título é obrigatório.")
            .AdicionarSe(tituloNormalizado.Length > TamanhoMaximoTitulo, "titulo", $"O título deve ter no máximo {TamanhoMaximoTitulo} caracteres.")
            .AdicionarSe(autorNormalizado.Length == 0, "autor", "O autor é obrigatório.")
            .AdicionarSe(autorNormalizado.Length > TamanhoMaximoAutor, "autor", $"O autor deve ter no máximo {TamanhoMaximoAutor} caracteres.")
            .AdicionarSe(isbnNormalizado.Length == 0, "isbn", "O ISBN é obrigatório.")
            .AdicionarSe(isbnNormalizado.Length is not 0 and not 10 and not 13, "isbn", "O ISBN deve conter 10 ou 13 caracteres (separadores são ignorados).")
            .AdicionarSe(anoPublicacao < AnoMinimoPublicacao || anoPublicacao > anoMaximo, "anoPublicacao", $"O ano de publicação deve estar entre {AnoMinimoPublicacao} e {anoMaximo}.")
            .AdicionarSe(categoriaId == Guid.Empty, "categoriaId", "A categoria é obrigatória.")
            .AdicionarSe(totalExemplares <= 0, "totalExemplares", "O total de exemplares deve ser maior que zero.")
            .AdicionarSe(totalExemplares > MaximoExemplares, "totalExemplares", $"O total de exemplares deve ser no máximo {MaximoExemplares}.")
            .LancarSeInvalido("Dados inválidos para o livro.");

        return (tituloNormalizado, autorNormalizado, isbnNormalizado);
    }
}
