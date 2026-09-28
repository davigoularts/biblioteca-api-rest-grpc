using Biblioteca.Dominio.Repositorios;
using Biblioteca.Repositorio.Memoria;
using Microsoft.Extensions.DependencyInjection;

namespace Biblioteca.Repositorio;

public static class RepositorioServiceCollectionExtensions
{
    public static IServiceCollection AdicionarRepositoriosEmMemoria(
        this IServiceCollection services,
        bool popularDadosIniciais = true)
    {
        services.AddSingleton(_ =>
        {
            var banco = new BancoEmMemoria();

            if (popularDadosIniciais)
            {
                SemeadorDeDados.Popular(banco);
            }

            return banco;
        });

        services.AddScoped<ICategoriaRepositorio, CategoriaRepositorioEmMemoria>();
        services.AddScoped<ILivroRepositorio, LivroRepositorioEmMemoria>();

        return services;
    }
}
