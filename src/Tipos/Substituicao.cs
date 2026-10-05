namespace Conde.Tipos;

/// <summary>
/// Uma SUBSTITUICAO: o que ja se descobriu sobre as variaveis de tipo.
///
/// Ela e um mapa de variavel para tipo, e aplicar ela a um tipo e trocar cada
/// variavel conhecida pelo que ela virou. A unificacao produz substituicoes, a
/// inferencia as acumula, e o tipo final e o resultado de aplicar tudo que se
/// descobriu ao tipo inicial.
///
/// O cuidado que importa esta na composicao: compor s2 depois de s1 nao e juntar
/// os dois mapas. O que s1 descobriu precisa ser atualizado com o que s2
/// descobriu depois, ou a inferencia devolve um tipo com buraco.
/// </summary>
public sealed class Substituicao
{
    private readonly Dictionary<int, Tipo> mapa;

    public Substituicao() => mapa = [];

    private Substituicao(Dictionary<int, Tipo> mapa) => this.mapa = mapa;

    public static Substituicao Vazia => new();

    public int Quantidade => mapa.Count;

    public static Substituicao De(int variavel, Tipo tipo) => new(new Dictionary<int, Tipo> { [variavel] = tipo });

    /// <summary>
    /// Aplica o que se sabe a um tipo.
    ///
    /// A troca e de UM PASSO: o que a variavel virou nao e aplicado de novo. A
    /// versao recursiva parece mais completa e nao e: com o teste de ocorrencia
    /// desligado, a substituicao pode mandar "a" para um tipo que contem "a", e
    /// aplicar recursivamente ali estoura a pilha em vez de produzir o tipo
    /// crescente que a medida quer mostrar.
    ///
    /// Encadeamento nao se perde por isso, porque a composicao ja atualiza o
    /// lado direito de cada entrada.
    /// </summary>
    public Tipo Aplicar(Tipo tipo)
    {
        ArgumentNullException.ThrowIfNull(tipo);

        return tipo switch
        {
            Variavel v => mapa.TryGetValue(v.Id, out var achado) ? achado : v,
            Seta s => new Seta(Aplicar(s.De), Aplicar(s.Para)),
            Par p => new Par(Aplicar(p.Esquerda), Aplicar(p.Direita)),
            _ => tipo,
        };
    }

    /// <summary>
    /// Aplica a um esquema, SEM mexer nas variaveis quantificadas.
    ///
    /// Isso nao e detalhe: as variaveis quantificadas sao ligadas ali, e trocar
    /// uma delas por causa de uma descoberta de fora e exatamente o bug de
    /// captura de variavel. A identidade deixaria de servir para qualquer tipo
    /// no momento em que alguem usasse ela num inteiro.
    /// </summary>
    public Esquema Aplicar(Esquema esquema)
    {
        ArgumentNullException.ThrowIfNull(esquema);
        if (esquema.Quantificados.Count == 0) return new Esquema(esquema.Quantificados, Aplicar(esquema.Corpo));

        var sem = new Dictionary<int, Tipo>(mapa);
        foreach (var ligada in esquema.Quantificados) sem.Remove(ligada);

        return new Esquema(esquema.Quantificados, new Substituicao(sem).Aplicar(esquema.Corpo));
    }

    /// <summary>
    /// A composicao: primeiro esta, depois a outra.
    ///
    /// O lado direito de cada entrada desta substituicao PASSA pela outra antes
    /// de entrar no resultado. Sem isso, uma variavel que esta aponta para b, e
    /// que a outra descobriu ser inteiro, continuaria apontando para b, e o tipo
    /// final sairia com um buraco.
    /// </summary>
    public Substituicao Depois(Substituicao outra)
    {
        ArgumentNullException.ThrowIfNull(outra);

        var junto = new Dictionary<int, Tipo>();
        foreach (var (variavel, tipo) in mapa) junto[variavel] = outra.Aplicar(tipo);
        foreach (var (variavel, tipo) in outra.mapa) junto.TryAdd(variavel, tipo);

        return new Substituicao(junto);
    }

    public bool Conhece(int variavel) => mapa.ContainsKey(variavel);

    public override string ToString() =>
        mapa.Count == 0 ? "{}" : "{" + string.Join(", ", mapa.Select(e => $"{Variavel.Nomear(e.Key)} := {e.Value}")) + "}";
}

/// <summary>
/// O AMBIENTE: o tipo de cada nome que esta no escopo.
///
/// Ele guarda ESQUEMAS, e nao tipos, e e isso que separa um nome preso a um tipo
/// de um nome que serve para todos.
/// </summary>
public sealed class Ambiente
{
    private readonly Dictionary<string, Esquema> mapa;

    public Ambiente() => mapa = [];

    private Ambiente(Dictionary<string, Esquema> mapa) => this.mapa = mapa;

    public Ambiente Com(string nome, Esquema esquema)
    {
        var novo = new Dictionary<string, Esquema>(mapa) { [nome] = esquema };
        return new Ambiente(novo);
    }

    public Esquema? Buscar(string nome) => mapa.TryGetValue(nome, out var achado) ? achado : null;

    public Ambiente Aplicar(Substituicao substituicao)
    {
        ArgumentNullException.ThrowIfNull(substituicao);

        var novo = new Dictionary<string, Esquema>();
        foreach (var (nome, esquema) in mapa) novo[nome] = substituicao.Aplicar(esquema);

        return new Ambiente(novo);
    }

    /// <summary>
    /// As variaveis de tipo que estao LIVRES no ambiente.
    ///
    /// Elas sao o que a generalizacao NAO pode quantificar: uma variavel que
    /// ainda aparece em outro lugar do escopo pode ser decidida depois, e
    /// quantificar ela aqui seria prometer uma generalidade que nao existe.
    /// </summary>
    public HashSet<int> VariaveisLivres()
    {
        var livres = new HashSet<int>();

        foreach (var esquema in mapa.Values)
            foreach (var variavel in esquema.Corpo.Variaveis())
                if (!esquema.Quantificados.Contains(variavel)) livres.Add(variavel);

        return livres;
    }
}
