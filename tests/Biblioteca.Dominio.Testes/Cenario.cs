using Biblioteca.Dominio.Servicos;
using Biblioteca.Repositorio.Memoria;

namespace Biblioteca.Dominio.Testes;

/// <summary>
/// Monta o domínio sem nenhuma dependência de ASP.NET, gRPC ou banco —
/// prova prática do baixo acoplamento entre camadas.
/// </summary>
internal sealed class Cenario
{
    private Cenario(BancoEmMemoria banco, ICategoriaService categorias, ILivroService livros)
    {
        Banco = banco;
        Categorias = categorias;
        Livros = livros;
    }

    public BancoEmMemoria Banco { get; }

    public ICategoriaService Categorias { get; }

    public ILivroService Livros { get; }

    public static Cenario Criar(bool popularDadosIniciais = true)
    {
        var banco = new BancoEmMemoria();

        if (popularDadosIniciais)
        {
            SemeadorDeDados.Popular(banco);
        }

        var categoriaRepositorio = new CategoriaRepositorioEmMemoria(banco);
        var livroRepositorio = new LivroRepositorioEmMemoria(banco);

        return new Cenario(
            banco,
            new CategoriaService(categoriaRepositorio, livroRepositorio),
            new LivroService(livroRepositorio, categoriaRepositorio));
    }
}
