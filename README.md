# Biblioteca API — REST + gRPC sobre o mesmo domínio

Sistema de gestão de acervo de biblioteca (**Categorias** e **Livros**) que expõe **exatamente os mesmos casos de uso** por REST e por gRPC, organizado em camadas **Apresentação → Domínio → Repositório**.

> **Regra central do trabalho:** um livro só pode ser vinculado a uma **categoria ativa**.
> Não é validação de formato — depende de consultar o outro agregado. É a regra equivalente ao “a marca precisa estar ativa” da VeiculosApi.

---

## 1. Como rodar

### Docker Compose (recomendado)

```bash
cd BibliotecaApi
docker compose up --build
```

| Serviço | Endereço |
|---|---|
| REST | `http://localhost:8080/api` |
| Swagger UI | `http://localhost:8080/swagger` |
| Health check | `http://localhost:8080/health` |
| gRPC (h2c, sem TLS) | `localhost:8081` |

Parar: `docker compose down`

### Só Docker

```bash
docker build -t biblioteca-api .
docker run --rm -p 8080:8080 -p 8081:8081 biblioteca-api
```

### Rodar os testes

```bash
# no host (precisa do SDK .NET 8)
dotnet test

# ou dentro do container, sem instalar nada
docker build --target test .
```

`30 testes` cobrindo todas as regras de negócio e o mapeamento de erros dos dois protocolos.

---

## 2. Arquitetura

```
┌──────────────────────── Apresentação (Biblioteca.Api) ────────────────────────┐
│                                                                              │
│  REST                                    gRPC                                │
│  Rest/Controllers/                       GrpcServices/                       │
│    CategoriasController                    CategoriaGrpcService              │
│    LivrosController                        LivroGrpcService                  │
│  Rest/Middleware/                        GrpcServices/Interceptors/          │
│    TratamentoDeExcecoesMiddleware          DominioExcecaoInterceptor         │
│           │                                        │                         │
└───────────┼────────────────────────────────────────┼─────────────────────────┘
            │      as DUAS bordas dependem das       │
            └────────► MESMAS interfaces ◄───────────┘
                             │
┌────────────────────────────▼─────────── Domínio (Biblioteca.Dominio) ────────┐
│  Servicos/     ICategoriaService, ILivroService   ← contratos dos casos de uso│
│                CategoriaService,  LivroService    ← regras de negócio (1x)    │
│  Entidades/    Categoria, Livro                   ← invariantes próprias      │
│  Excecoes/     DominioException + 4 especializações                           │
│  Contratos/    DTOs de entrada e saída                                       │
│  Repositorios/ ICategoriaRepositorio, ILivroRepositorio  ← portas de saída    │
└────────────────────────────┬─────────────────────────────────────────────────┘
                             │
┌────────────────────────────▼──────── Repositório (Biblioteca.Repositorio) ────┐
│  Memoria/  CategoriaRepositorioEmMemoria, LivroRepositorioEmMemoria           │
│            BancoEmMemoria (ConcurrentDictionary), SemeadorDeDados             │
└──────────────────────────────────────────────────────────────────────────────┘
```

Dependências apontam **sempre para dentro**: `Api → Dominio ← Repositorio`.
O domínio não referencia ASP.NET, gRPC nem banco — por isso os testes montam o sistema
inteiro com `new CategoriaService(...)`, sem host web (ver `tests/.../Cenario.cs`).

### Estrutura de pastas

```
BibliotecaApi/
├─ Biblioteca.sln
├─ Dockerfile                  # multi-stage: build → test (opcional) → runtime
├─ docker-compose.yml
├─ postman/
│  ├─ Biblioteca.postman_collection.json     # 26 requisições com testes automáticos
│  └─ Biblioteca.postman_environment.json
├─ src/
│  ├─ Biblioteca.Dominio/      # camada de domínio (zero dependência de framework)
│  ├─ Biblioteca.Repositorio/  # camada de persistência (em memória)
│  └─ Biblioteca.Api/          # camada de apresentação (REST + gRPC)
│     └─ Protos/biblioteca.proto
└─ tests/
   └─ Biblioteca.Dominio.Testes/
```

---

## 3. Reaproveitamento de domínio (o ponto central)

A mesma interface é injetada nas duas bordas. Nenhuma regra é reescrita.

**REST** — `src/Biblioteca.Api/Rest/Controllers/LivrosController.cs`

```csharp
public sealed class LivrosController : ControllerBase
{
    private readonly ILivroService _livros;           // ◄── mesma interface

    public LivrosController(ILivroService livros) => _livros = livros;

    [HttpPost]
    public async Task<ActionResult<LivroDto>> Criar([FromBody] CriarLivroDto? dados, CancellationToken ct)
    {
        var criado = await _livros.CriarAsync(dados!, ct);   // ◄── mesma chamada
        return CreatedAtRoute(nameof(ObterLivroPorId), new { id = criado.Id }, criado);
    }
}
```

**gRPC** — `src/Biblioteca.Api/GrpcServices/LivroGrpcService.cs`

```csharp
public sealed class LivroGrpcService : ContratoLivro.LivroServiceBase
{
    private readonly IServicoDeLivro _livros;         // ◄── mesma interface (ILivroService)

    public LivroGrpcService(IServicoDeLivro livros) => _livros = livros;

    public override async Task<LivroMessage> Criar(CriarLivroRequest request, ServerCallContext context)
    {
        var dados = new CriarLivroDto(
            request.Titulo, request.Autor, request.Isbn, request.AnoPublicacao,
            MapeadorGrpc.ParaGuid(request.CategoriaId, "categoriaId"), request.TotalExemplares);

        return MapeadorGrpc.ParaMensagem(
            await _livros.CriarAsync(dados, context.CancellationToken));   // ◄── mesma chamada
    }
}
```

Registro único em `Program.cs`:

```csharp
builder.Services
    .AdicionarDominio()                 // ICategoriaService/ILivroService  (Biblioteca.Dominio)
    .AdicionarRepositoriosEmMemoria();  // ICategoriaRepositorio/ILivroRepositorio (Biblioteca.Repositorio)
```

Até a **validação de campos** é do domínio: os controllers não usam `[Required]`/`[StringLength]`
(`SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true`), então REST e gRPC
reportam exatamente os mesmos erros, com as mesmas mensagens.

---

## 4. Regras de negócio

Regras marcadas com ★ **cruzam os dois agregados** (precisam consultar o outro para decidir).

| # | Regra | Exceção | HTTP | gRPC |
|---|---|---|---|---|
| ★ **R1** | Livro só pode ser vinculado a categoria **ativa** (no cadastro e ao trocar de categoria) | `RegraDeNegocioException` `LIVRO_CATEGORIA_INATIVA` | 409 | `FailedPrecondition` |
| ★ R2 | Categoria com livros que têm exemplares emprestados não pode ser inativada | `RegraDeNegocioException` `CATEGORIA_COM_EMPRESTIMO_ATIVO` | 409 | `FailedPrecondition` |
| ★ R3 | Categoria com livros vinculados não pode ser removida | `RegraDeNegocioException` `CATEGORIA_COM_LIVROS_VINCULADOS` | 409 | `FailedPrecondition` |
| R4 | ISBN é único no acervo (separadores ignorados: `978-0-13-449416-6` = `9780134494166`) | `RecursoDuplicadoException` | 409 | `AlreadyExists` |
| R5 | Nome de categoria é único (case-insensitive) | `RecursoDuplicadoException` | 409 | `AlreadyExists` |
| R6 | Não se empresta sem exemplar disponível | `RegraDeNegocioException` `LIVRO_SEM_EXEMPLAR_DISPONIVEL` | 409 | `FailedPrecondition` |
| ★ R7 | Empréstimo exige que a categoria do livro esteja ativa | `RegraDeNegocioException` `LIVRO_CATEGORIA_INATIVA` | 409 | `FailedPrecondition` |
| R8 | Livro com exemplar emprestado não pode ser removido | `RegraDeNegocioException` `LIVRO_COM_EMPRESTIMO_ATIVO` | 409 | `FailedPrecondition` |
| R9 | Total de exemplares não pode ficar abaixo do que está emprestado | `RegraDeNegocioException` | 409 | `FailedPrecondition` |

Todas vivem **só** em `CategoriaService` / `LivroService` (regras entre agregados) e nas entidades
(invariantes próprias). Nenhum `if` de regra nos controllers ou nos serviços gRPC.

---

## 5. Tratamento de exceções

O domínio lança uma exceção que **não conhece protocolo**:

```csharp
// Biblioteca.Dominio/Servicos/LivroService.cs
private async Task<Categoria> BuscarCategoriaAtivaAsync(Guid categoriaId, CancellationToken ct, string? msg = null)
{
    var categoria = await _categorias.ObterPorIdAsync(categoriaId, ct)
                    ?? throw new RecursoNaoEncontradoException("Categoria", categoriaId);

    if (!categoria.Ativa)
    {
        throw new RegraDeNegocioException(
            msg ?? $"A categoria '{categoria.Nome}' está inativa; não é possível vincular livros a ela.",
            "LIVRO_CATEGORIA_INATIVA");
    }

    return categoria;
}
```

Cada borda traduz com **um único ponto de decisão**:

| Exceção de domínio | HTTP (`TratamentoDeExcecoesMiddleware`) | gRPC (`DominioExcecaoInterceptor`) |
|---|---|---|
| `ValidacaoException` | **400** Bad Request | `InvalidArgument` |
| `RecursoNaoEncontradoException` | **404** Not Found | `NotFound` |
| `RecursoDuplicadoException` | **409** Conflict | `AlreadyExists` |
| `RegraDeNegocioException` | **409** Conflict | `FailedPrecondition` |
| qualquer outra | **500** | `Internal` |

O teste `TraducaoDeExcecoesTestes` compara as duas tabelas lado a lado, garantindo que as bordas
não divirjam.

### Formato de erro REST (RFC 7807 — `application/problem+json`)

```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Conflito com o estado atual",
  "status": 409,
  "detail": "A categoria 'Periódicos Descontinuados' está inativa; não é possível vincular livros a ela.",
  "instance": "/api/livros",
  "traceId": "00-6ded4ec9c72acfcbad5767c4849fd249-ceaea79603948be8-00",
  "codigo": "REGRA_DE_NEGOCIO",
  "regra": "LIVRO_CATEGORIA_INATIVA"
}
```

Erro de validação agrega todos os campos de uma vez:

```json
{
  "status": 400,
  "detail": "Dados inválidos para o livro.",
  "codigo": "VALIDACAO",
  "erros": {
    "titulo": ["O título é obrigatório."],
    "isbn": ["O ISBN deve conter 10 ou 13 caracteres (separadores são ignorados)."],
    "anoPublicacao": ["O ano de publicação deve estar entre 1400 e 2027."],
    "totalExemplares": ["O total de exemplares deve ser maior que zero."]
  }
}
```

### Formato de erro gRPC

```
Code: InvalidArgument
Message: Dados inválidos para o livro. [titulo: O título é obrigatório. | isbn: O ISBN deve conter 10 ou 13 caracteres ...]

Response trailers:
  codigo: VALIDACAO
  campos-invalidos: titulo,isbn,anoPublicacao,totalExemplares
```

> Trailers gRPC viajam como cabeçalhos HTTP/2 e aceitam **apenas ASCII imprimível** — texto
> acentuado neles derruba a resposta com 500. Por isso as mensagens vão no *status* (que o
> protocolo transporta percent-encoded) e os trailers levam só códigos e nomes de campo.
> Coberto pelo teste `Trailers_gRPC_devem_conter_apenas_ascii_imprimivel`.

---

## 6. Endpoints REST

### Categorias

| Método | Rota | Descrição | Status |
|---|---|---|---|
| GET | `/api/categorias?apenasAtivas=false` | Lista categorias (com `totalLivros`) | 200 |
| GET | `/api/categorias/{id}` | Detalhe | 200, 404 |
| POST | `/api/categorias` | Cadastra | 201, 400, 409 |
| PUT | `/api/categorias/{id}` | Atualiza nome/descrição | 200, 400, 404, 409 |
| PATCH | `/api/categorias/{id}/ativar` | Ativa | 200, 404 |
| PATCH | `/api/categorias/{id}/inativar` | Inativa (**R2**) | 200, 404, 409 |
| DELETE | `/api/categorias/{id}` | Remove (**R3**) | 204, 404, 409 |

### Livros

| Método | Rota | Descrição | Status |
|---|---|---|---|
| GET | `/api/livros?categoriaId=&termo=` | Lista/filtra | 200 |
| GET | `/api/livros/{id}` | Detalhe | 200, 404 |
| POST | `/api/livros` | Cadastra (**R1**, **R4**) | 201, 400, 404, 409 |
| PUT | `/api/livros/{id}` | Atualiza (**R1**, **R4**, **R9**) | 200, 400, 404, 409 |
| DELETE | `/api/livros/{id}` | Remove (**R8**) | 204, 404, 409 |
| POST | `/api/livros/{id}/emprestimos` | Empresta 1 exemplar (**R6**, **R7**) | 200, 404, 409 |
| POST | `/api/livros/{id}/devolucoes` | Devolve 1 exemplar | 200, 404, 409 |

### Exemplos

```bash
# Feliz
curl http://localhost:8080/api/categorias

curl -X POST http://localhost:8080/api/livros \
  -H "Content-Type: application/json" \
  -d '{"titulo":"Duna","autor":"Frank Herbert","isbn":"9780441013593",
       "anoPublicacao":1965,"categoriaId":"11111111-1111-1111-1111-111111111111",
       "totalExemplares":2}'

# R1 → 409 (categoria 3333... está inativa na carga inicial)
curl -i -X POST http://localhost:8080/api/livros \
  -H "Content-Type: application/json" \
  -d '{"titulo":"Revista Byte","autor":"Diversos","isbn":"0360528401",
       "anoPublicacao":1990,"categoriaId":"33333333-3333-3333-3333-333333333333",
       "totalExemplares":1}'
```

---

## 7. gRPC

Contrato: [`src/Biblioteca.Api/Protos/biblioteca.proto`](src/Biblioteca.Api/Protos/biblioteca.proto)
Porta **8081**, HTTP/2 em texto claro (h2c), **server reflection habilitado**.

| `biblioteca.v1.CategoriaService` | `biblioteca.v1.LivroService` |
|---|---|
| `Listar`, `ObterPorId`, `Criar`, `Atualizar`, `Ativar`, `Inativar`, `Remover` | `Listar`, `ObterPorId`, `Criar`, `Atualizar`, `Remover`, `Emprestar`, `Devolver` |

### Testando no Postman

1. **New → gRPC Request**
2. URL: `localhost:8081` — deixe **Enable TLS desligado**
3. Em *Service definition*, escolha **Using server reflection** (ou *Import a .proto file* e aponte para `src/Biblioteca.Api/Protos/biblioteca.proto`)
4. Escolha o método e cole a mensagem. Exemplos:

`CategoriaService/Listar`
```json
{ "apenas_ativas": true }
```

`LivroService/Criar` — caminho feliz
```json
{
  "titulo": "Duna",
  "autor": "Frank Herbert",
  "isbn": "9780441013593",
  "ano_publicacao": 1965,
  "categoria_id": "11111111-1111-1111-1111-111111111111",
  "total_exemplares": 2
}
```

`LivroService/Criar` — **R1 → FailedPrecondition**
```json
{
  "titulo": "Revista Byte",
  "autor": "Diversos",
  "isbn": "0360528401",
  "ano_publicacao": 1990,
  "categoria_id": "33333333-3333-3333-3333-333333333333",
  "total_exemplares": 1
}
```

`LivroService/Emprestar` — **R6 → FailedPrecondition** (único exemplar já emprestado)
```json
{ "id": "bbbbbbbb-0000-0000-0000-000000000002" }
```

`CategoriaService/Inativar` — **R2 → FailedPrecondition**
```json
{ "id": "22222222-2222-2222-2222-222222222222" }
```

`CategoriaService/ObterPorId` — **NotFound**
```json
{ "id": "99999999-9999-9999-9999-999999999999" }
```

### Testando por linha de comando (grpcurl)

```bash
docker run --rm --network bibliotecaapi_default fullstorydev/grpcurl:latest \
  -plaintext biblioteca-api:8081 list

docker run --rm --network bibliotecaapi_default fullstorydev/grpcurl:latest \
  -plaintext -d '{"apenas_ativas":true}' \
  biblioteca-api:8081 biblioteca.v1.CategoriaService/Listar
```

---

## 8. Testando pelo Postman (REST)

1. **Import** → `postman/Biblioteca.postman_collection.json`
2. **Import** → `postman/Biblioteca.postman_environment.json` e selecione o environment *Biblioteca - Local (Docker)*
3. **Runner** → rode a coleção inteira, na ordem das pastas:

| Pasta | Conteúdo |
|---|---|
| `00 - Health` | disponibilidade |
| `01 - Fluxos felizes` | CRUD de categoria e livro, filtros, empréstimo/devolução, ativar/inativar |
| `02 - Regras de negócio (erros)` | **R1, R2, R3, R4, R6, R8**, 404 e 400 — cada requisição valida status *e* o `codigo`/`regra` do ProblemDetails |
| `03 - Limpeza` | remove o que foi criado (204) |

Todas as requisições têm scripts de teste (`pm.test`), então o Runner mostra aprovado/reprovado
por regra. Os ids da carga inicial já vêm nas variáveis da coleção.

---

## 9. Dados iniciais

Carregados em memória no start (`SemeadorDeDados`), com ids fixos para facilitar os testes.

**Categorias**

| Id | Nome | Ativa |
|---|---|---|
| `11111111-1111-1111-1111-111111111111` | Tecnologia | ✅ |
| `22222222-2222-2222-2222-222222222222` | Literatura Brasileira | ✅ |
| `33333333-3333-3333-3333-333333333333` | Periódicos Descontinuados | ❌ **(usada para demonstrar R1)** |

**Livros**

| Id | Título | Categoria | Exemplares |
|---|---|---|---|
| `aaaaaaaa-0000-0000-0000-000000000001` | Clean Architecture | Tecnologia | 4 (0 emprestados) |
| `aaaaaaaa-0000-0000-0000-000000000002` | Domain-Driven Design | Tecnologia | 2 (0 emprestados) |
| `bbbbbbbb-0000-0000-0000-000000000001` | Dom Casmurro | Literatura Brasileira | 3 (0 emprestados) |
| `bbbbbbbb-0000-0000-0000-000000000002` | Grande Sertão: Veredas | Literatura Brasileira | 1 (**1 emprestado** → demonstra R2, R6 e R8) |

> Como o repositório é em memória, `docker compose restart` devolve a base ao estado inicial.
> Trocar por banco real significa escrever outra implementação de `ICategoriaRepositorio` /
> `ILivroRepositorio` — sem tocar no domínio nem na apresentação.

---

## 10. Atendimento ao enunciado

| Requisito | Onde está |
|---|---|
| API expõe os mesmos casos de uso por REST e gRPC | `Rest/Controllers/` e `GrpcServices/` chamando as mesmas `ICategoriaService`/`ILivroService` |
| Camadas Apresentação → Domínio → Repositório | 3 projetos: `Biblioteca.Api`, `Biblioteca.Dominio`, `Biblioteca.Repositorio` |
| Domínio não é veículos/marcas | Biblioteca: Categorias e Livros |
| 2 entidades com regra que cruza os agregados | `Categoria` × `Livro`; R1, R2, R3 e R7 exigem consultar o outro agregado |
| Regra não é validação de formato | R1 consulta `ICategoriaRepositorio` para checar `Ativa`; nenhum `[Required]`/`[StringLength]` nos DTOs |
| Regra de negócio não duplicada | `LivroService`/`CategoriaService` são o único lugar com regra; as bordas só traduzem |
| Exceção de domínio (sem `if` decidindo status no controller) | `DominioException` e as 4 especializações em `Biblioteca.Dominio/Excecoes/` |
| Tradução para HTTP | `TratamentoDeExcecoesMiddleware` → 400 / 404 / 409 / 500 |
| Tradução para gRPC | `DominioExcecaoInterceptor` → `InvalidArgument` / `NotFound` / `AlreadyExists` / `FailedPrecondition` / `Internal` |
| Sobe via Docker/docker-compose | `Dockerfile` (multi-stage) + `docker-compose.yml` |
| Testável pelo Postman | `postman/` — coleção REST com testes automáticos + roteiro gRPC na seção 7 |
