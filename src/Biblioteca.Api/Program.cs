using System.Reflection;
using System.Text.Json;
using Biblioteca.Api.GrpcServices;
using Biblioteca.Api.GrpcServices.Interceptors;
using Biblioteca.Api.Rest.Middleware;
using Biblioteca.Dominio;
using Biblioteca.Repositorio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Kestrel: REST em HTTP/1.1 e gRPC em HTTP/2 (h2c) em portas separadas, para que
// o Postman consiga falar com os dois sem TLS.
// ---------------------------------------------------------------------------
var portaRest = builder.Configuration.GetValue("Portas:Rest", 8080);
var portaGrpc = builder.Configuration.GetValue("Portas:Grpc", 8081);

builder.WebHost.ConfigureKestrel(opcoes =>
{
    opcoes.ListenAnyIP(portaRest, escuta => escuta.Protocols = HttpProtocols.Http1AndHttp2);
    opcoes.ListenAnyIP(portaGrpc, escuta => escuta.Protocols = HttpProtocols.Http2);
});

// ---------------------------------------------------------------------------
// Composição das camadas: Apresentação -> Domínio -> Repositório.
// A apresentação não conhece a implementação do repositório; o domínio não
// conhece nenhuma das duas bordas.
// ---------------------------------------------------------------------------
builder.Services
    .AdicionarDominio()
    .AdicionarRepositoriosEmMemoria();

// ------------------------------- Apresentação REST -------------------------
builder.Services
    .AddControllers(opcoes =>
        // Validação é responsabilidade do domínio: sem [Required] implícito na borda,
        // REST e gRPC reportam exatamente os mesmos erros.
        opcoes.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        opcoes.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.Configure<ApiBehaviorOptions>(opcoes =>
    // Corpo ausente/ilegível também vira ValidacaoException do domínio,
    // mantendo um único formato de erro (ProblemDetails) em toda a API.
    opcoes.SuppressModelStateInvalidFilter = true);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Biblioteca API",
        Version = "v1",
        Description =
            "Gestão de acervo (Categorias e Livros) exposta por REST e gRPC sobre o mesmo domínio. " +
            "Regra central: um livro só pode ser vinculado a uma categoria ativa.",
    });

    var xml = Path.Combine(
        AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");

    if (File.Exists(xml))
    {
        opcoes.IncludeXmlComments(xml);
    }
});

// ------------------------------- Apresentação gRPC -------------------------
builder.Services.AddGrpc(opcoes =>
{
    opcoes.EnableDetailedErrors = true;
    opcoes.Interceptors.Add<DominioExcecaoInterceptor>();
});

builder.Services.AddGrpcReflection();

var app = builder.Build();

// Tradutor HTTP das exceções de domínio: precisa ser o primeiro da pipeline.
app.UseMiddleware<TratamentoDeExcecoesMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(opcoes =>
{
    opcoes.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API v1");
    opcoes.DocumentTitle = "Biblioteca API";
});

app.MapControllers();

app.MapGrpcService<CategoriaGrpcService>();
app.MapGrpcService<LivroGrpcService>();

// Reflection permite que o Postman/grpcurl descubram os serviços sem importar o .proto.
app.MapGrpcReflectionService();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    utc = DateTime.UtcNow,
})).ExcludeFromDescription();

app.Logger.LogInformation(
    "Biblioteca API pronta. REST/Swagger em http://localhost:{PortaRest} | gRPC (h2c) em http://localhost:{PortaGrpc}",
    portaRest,
    portaGrpc);

app.Run();
