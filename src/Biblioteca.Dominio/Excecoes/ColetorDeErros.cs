namespace Biblioteca.Dominio.Excecoes;

/// <summary>
/// Acumula erros de validação para que a API responda com todos os problemas de uma vez,
/// em vez de devolver um erro por requisição.
/// </summary>
public sealed class ColetorDeErros
{
    private readonly Dictionary<string, List<string>> _erros = new(StringComparer.OrdinalIgnoreCase);

    public bool PossuiErros => _erros.Count > 0;

    public ColetorDeErros Adicionar(string campo, string mensagem)
    {
        if (!_erros.TryGetValue(campo, out var lista))
        {
            lista = new List<string>();
            _erros[campo] = lista;
        }

        lista.Add(mensagem);
        return this;
    }

    public ColetorDeErros AdicionarSe(bool condicao, string campo, string mensagem)
        => condicao ? Adicionar(campo, mensagem) : this;

    public void LancarSeInvalido(string mensagem)
    {
        if (!PossuiErros)
        {
            return;
        }

        var detalhes = _erros.ToDictionary(par => par.Key, par => par.Value.ToArray());
        throw new ValidacaoException(mensagem, detalhes);
    }
}
