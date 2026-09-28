using Biblioteca.Dominio.Servicos;
using Microsoft.Extensions.DependencyInjection;

namespace Biblioteca.Dominio;

public static class DominioServiceCollectionExtensions
{
    /// <summary>
    /// Registra os casos de uso. Como REST e gRPC resolvem as mesmas interfaces,
    /// a regra de negócio existe uma única vez no processo.
    /// </summary>
    public static IServiceCollection AdicionarDominio(this IServiceCollection services)
    {
        services.AddScoped<ICategoriaService, CategoriaService>();
        services.AddScoped<ILivroService, LivroService>();
        return services;
    }
}
