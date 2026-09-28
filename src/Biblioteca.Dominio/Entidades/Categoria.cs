using Biblioteca.Dominio.Excecoes;

namespace Biblioteca.Dominio.Entidades;

/// <summary>
/// Agregado Categoria. Guarda apenas as invariantes que dependem de si mesma;
/// regras que cruzam Categoria x Livro ficam nos serviços de domínio, que enxergam
/// os dois agregados.
/// </summary>
public sealed class Categoria
{
    public const int TamanhoMaximoNome = 80;
    public const int TamanhoMaximoDescricao = 300;

    private Categoria(Guid id, string nome, string descricao, bool ativa, DateTime criadaEm)
    {
        Id = id;
        Nome = nome;
        Descricao = descricao;
        Ativa = ativa;
        CriadaEm = criadaEm;
    }

    public Guid Id { get; }

    public string Nome { get; private set; }

    public string Descricao { get; private set; }

    /// <summary>Somente categorias ativas podem receber livros e liberar empréstimos.</summary>
    public bool Ativa { get; private set; }

    public DateTime CriadaEm { get; }

    public static Categoria Criar(string? nome, string? descricao, bool ativa)
    {
        var (nomeValidado, descricaoValidada) = Validar(nome, descricao);
        return new Categoria(Guid.NewGuid(), nomeValidado, descricaoValidada, ativa, DateTime.UtcNow);
    }

    /// <summary>Reconstrói a entidade a partir do repositório (ou da carga inicial de dados).</summary>
    public static Categoria Reidratar(Guid id, string nome, string descricao, bool ativa, DateTime criadaEm)
        => new(id, nome, descricao, ativa, criadaEm);

    public void Atualizar(string? nome, string? descricao)
    {
        var (nomeValidado, descricaoValidada) = Validar(nome, descricao);
        Nome = nomeValidado;
        Descricao = descricaoValidada;
    }

    public void Ativar() => Ativa = true;

    public void Inativar() => Ativa = false;

    public static string NormalizarNome(string? nome) => (nome ?? string.Empty).Trim();

    private static (string Nome, string Descricao) Validar(string? nome, string? descricao)
    {
        var nomeNormalizado = NormalizarNome(nome);
        var descricaoNormalizada = (descricao ?? string.Empty).Trim();

        new ColetorDeErros()
            .AdicionarSe(nomeNormalizado.Length == 0, "nome", "O nome da categoria é obrigatório.")
            .AdicionarSe(nomeNormalizado.Length > TamanhoMaximoNome, "nome", $"O nome da categoria deve ter no máximo {TamanhoMaximoNome} caracteres.")
            .AdicionarSe(descricaoNormalizada.Length > TamanhoMaximoDescricao, "descricao", $"A descrição deve ter no máximo {TamanhoMaximoDescricao} caracteres.")
            .LancarSeInvalido("Dados inválidos para a categoria.");

        return (nomeNormalizado, descricaoNormalizada);
    }
}
