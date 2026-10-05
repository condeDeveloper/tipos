namespace Conde.Tipos;

/// <summary>
/// Os programas que valem a pena olhar um por um.
///
/// Cada um deles existe para mostrar uma coisa so, e os nomes dizem qual. Os
/// interessantes nao sao os que tipam: sao os tres do fim, que separam o que o
/// sistema aceita do que de fato funciona.
/// </summary>
public static class Exemplos
{
    public static Termo Fun(string parametro, Termo corpo) => new Funcao(parametro, corpo);

    public static Termo Ap(Termo alvo, params Termo[] argumentos) =>
        argumentos.Aggregate(alvo, (atual, argumento) => new Aplicacao(atual, argumento));

    public static Termo Var(string nome) => new Nome(nome);

    public static Termo Num(int valor) => new Numero(valor);

    public static Termo Log(bool valor) => new Booleano(valor);

    /// <summary>fun x -> x. O tipo principal dela e "para todo a. a -> a".</summary>
    public static Termo Identidade() => Fun("x", Var("x"));

    /// <summary>fun x -> fun y -> x, que joga o segundo argumento fora.</summary>
    public static Termo Constante() => Fun("x", Fun("y", Var("x")));

    /// <summary>A composicao: fun f -> fun g -> fun x -> f (g x).</summary>
    public static Termo Composicao() =>
        Fun("f", Fun("g", Fun("x", Ap(Var("f"), Ap(Var("g"), Var("x"))))));

    /// <summary>
    /// let id = fun x -> x in (id 1, id verdadeiro).
    ///
    /// E o programa que so tipa com let-polimorfismo. Sem generalizacao, o
    /// primeiro uso prende "id" a inteiro e o segundo e erro de tipo, mesmo com
    /// o programa rodando perfeitamente.
    /// </summary>
    public static Termo IdentidadeNosDoisTipos() =>
        new Deixe("id", Identidade(), new Dupla(Ap(Var("id"), Num(1)), Ap(Var("id"), Log(true))));

    /// <summary>
    /// A MESMA conta escrita com aplicacao em vez de let.
    ///
    /// Ela roda exatamente igual e NAO tipa, nem com polimorfismo: so o let
    /// generaliza. Esse par de programas e a demonstracao mais curta de que o
    /// sistema de tipos recusa programa que funciona.
    /// </summary>
    public static Termo IdentidadeNosDoisTiposSemLet() =>
        Ap(Fun("id", new Dupla(Ap(Var("id"), Num(1)), Ap(Var("id"), Log(true)))), Identidade());

    /// <summary>
    /// fun x -> x x, a auto-aplicacao.
    ///
    /// Ela e o caso do teste de ocorrencia: tipar ela exigiria "a" igual a
    /// "a -> b", que e um tipo infinito.
    /// </summary>
    public static Termo AutoAplicacao() => Fun("x", Ap(Var("x"), Var("x")));

    /// <summary>
    /// O combinador OMEGA: (fun x -> x x) (fun x -> x x).
    ///
    /// Ele nao tipa e tambem nao termina. E o unico caso deste repositorio em
    /// que recusar o programa foi mesmo a coisa certa.
    /// </summary>
    public static Termo Omega() => Ap(AutoAplicacao(), AutoAplicacao());

    /// <summary>1 + verdadeiro, o erro de tipo mais simples que existe.</summary>
    public static Termo SomaComLogico() => new Soma(Num(1), Log(true));

    /// <summary>Aplicar um numero, que nao e funcao.</summary>
    public static Termo AplicarNumero() => Ap(Num(1), Num(2));

    /// <summary>se 1 entao 2 senao 3: desviar por algo que nao e logico.</summary>
    public static Termo DesvioPorNumero() => new Se(Num(1), Num(2), Num(3));

    /// <summary>Os dois ramos do se com tipos diferentes.</summary>
    public static Termo RamosDiferentes() => new Se(Log(true), Num(1), Log(false));

    /// <summary>
    /// se verdadeiro entao 1 senao (1 + verdadeiro).
    ///
    /// Ele NAO tipa, e roda sem travar: o ramo com erro nunca e executado. O
    /// sistema de tipos olha os dois caminhos, e o programa so anda por um.
    /// </summary>
    public static Termo RamoRuimQueNuncaRoda() =>
        new Se(Log(true), Num(1), new Soma(Num(1), Log(true)));

    /// <summary>
    /// (fun x -> 42) (1 + verdadeiro).
    ///
    /// Tambem nao tipa e tambem nao trava, porque este avaliador e PREGUICOSO
    /// com o corpo e nao com o argumento... ou seria, se fosse. Ele avalia o
    /// argumento antes, entao este trava: e o exemplo que mostra que "recusa
    /// programa bom" depende da ordem de avaliacao, e nao so do tipo.
    /// </summary>
    public static Termo ArgumentoRuimUsadoOuNao() =>
        Ap(Fun("x", Num(42)), new Soma(Num(1), Log(true)));

    /// <summary>
    /// A TORRE DE PARES: let p0 = fun x -> x in let p1 = (p0, p0) in ...
    ///
    /// Cada let dobra o tamanho do tipo, entao o tipo inferido cresce
    /// exponencialmente com o tamanho do programa. Essa e a razao de verdade de
    /// a inferencia de Hindley e Milner ser cara no pior caso, e ela nao esta no
    /// algoritmo: esta no TAMANHO DA RESPOSTA.
    /// </summary>
    public static Termo TorreDePares(int andares)
    {
        Termo corpo = Var($"p{andares}");

        for (var i = andares; i >= 1; i--)
            corpo = new Deixe($"p{i}", new Dupla(Var($"p{i - 1}"), Var($"p{i - 1}")), corpo);

        return new Deixe("p0", Identidade(), corpo);
    }

    /// <summary>Os programas que devem tipar.</summary>
    public static IEnumerable<(string Nome, Termo Termo)> QueTipam()
    {
        yield return ("identidade", Identidade());
        yield return ("constante", Constante());
        yield return ("composicao", Composicao());
        yield return ("id nos dois tipos", IdentidadeNosDoisTipos());
        yield return ("soma", new Soma(Num(1), Num(2)));
        yield return ("par", new Dupla(Num(1), Log(true)));
        yield return ("projecao", new Projecao(true, new Dupla(Num(1), Log(true))));
        yield return ("se", new Se(Log(true), Num(1), Num(2)));
        yield return ("torre de 3", TorreDePares(3));
    }

    /// <summary>Os programas que devem ser recusados.</summary>
    public static IEnumerable<(string Nome, Termo Termo)> QueNaoTipam()
    {
        yield return ("soma com logico", SomaComLogico());
        yield return ("aplicar numero", AplicarNumero());
        yield return ("desvio por numero", DesvioPorNumero());
        yield return ("ramos diferentes", RamosDiferentes());
        yield return ("auto-aplicacao", AutoAplicacao());
        yield return ("omega", Omega());
        yield return ("id nos dois tipos sem let", IdentidadeNosDoisTiposSemLet());
        yield return ("ramo ruim que nunca roda", RamoRuimQueNuncaRoda());
    }
}
