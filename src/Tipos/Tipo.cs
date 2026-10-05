namespace Conde.Tipos;

/// <summary>
/// Um TIPO.
///
/// Sao quatro formas e nada mais: uma constante como inteiro ou logico, uma
/// VARIAVEL de tipo, que e o "ainda nao sei", uma seta de um tipo para outro, e
/// um par. Com isso da para descrever toda funcao deste repositorio, e o pouco
/// que falta e exatamente o que mantem a inferencia decidivel.
/// </summary>
public abstract record Tipo
{
    public static readonly Tipo Inteiro = new Constante("int");

    public static readonly Tipo Logico = new Constante("bool");

    /// <summary>As variaveis de tipo que aparecem dentro deste tipo.</summary>
    public IEnumerable<int> Variaveis()
    {
        switch (this)
        {
            case Variavel v:
                yield return v.Id;
                break;

            case Seta s:
                foreach (var i in s.De.Variaveis()) yield return i;
                foreach (var i in s.Para.Variaveis()) yield return i;
                break;

            case Par p:
                foreach (var i in p.Esquerda.Variaveis()) yield return i;
                foreach (var i in p.Direita.Variaveis()) yield return i;
                break;
        }
    }

    /// <summary>
    /// O TAMANHO do tipo, contado em nos.
    ///
    /// Ele parece um detalhe e nao e: o tamanho do tipo inferido pode crescer
    /// EXPONENCIALMENTE com o tamanho do programa, e e esse crescimento, e nao
    /// a dificuldade do algoritmo, que faz a inferencia de Hindley e Milner ser
    /// cara no pior caso.
    /// </summary>
    public int Tamanho() => this switch
    {
        Seta s => 1 + s.De.Tamanho() + s.Para.Tamanho(),
        Par p => 1 + p.Esquerda.Tamanho() + p.Direita.Tamanho(),
        _ => 1,
    };
}

public sealed record Constante(string Nome) : Tipo
{
    public override string ToString() => Nome;
}

/// <summary>
/// Uma VARIAVEL de tipo: o lugar onde o algoritmo ainda nao decidiu nada.
///
/// Toda a inferencia e a historia de preencher essas lacunas. Quando a conta
/// acaba e uma delas continua vazia, isso nao e falha: e POLIMORFISMO, e quer
/// dizer que a funcao serve para qualquer tipo ali.
/// </summary>
public sealed record Variavel(int Id) : Tipo
{
    public override string ToString() => Nomear(Id);

    /// <summary>a, b, c... e depois a1, b1, para o tipo ficar legivel.</summary>
    public static string Nomear(int id)
    {
        var letra = (char)('a' + id % 26);
        var volta = id / 26;
        return volta == 0 ? letra.ToString() : $"{letra}{volta}";
    }
}

public sealed record Seta(Tipo De, Tipo Para) : Tipo
{
    // O lado esquerdo de uma seta ganha parenteses e o direito nao, porque a
    // seta associa a direita: a -> b -> c e a -> (b -> c).
    public override string ToString() => $"{(De is Seta ? $"({De})" : De.ToString())} -> {Para}";
}

public sealed record Par(Tipo Esquerda, Tipo Direita) : Tipo
{
    public override string ToString() => $"({Esquerda}, {Direita})";
}

/// <summary>
/// Um ESQUEMA: um tipo com variaveis QUANTIFICADAS, que e o que faz o
/// polimorfismo existir.
///
/// A diferenca entre "a -> a" e "para todo a, a -> a" e a diferenca entre uma
/// funcao cujo tipo ainda sera decidido e uma funcao que serve para todos os
/// tipos. Sem essa distincao, usar a identidade em inteiro e depois em logico
/// no mesmo programa seria erro de tipo, e a medida mostra quantos programas
/// isso derruba.
/// </summary>
public sealed record Esquema(IReadOnlyList<int> Quantificados, Tipo Corpo)
{
    public static Esquema Simples(Tipo tipo) => new(Array.Empty<int>(), tipo);

    public override string ToString() =>
        Quantificados.Count == 0
            ? Corpo.ToString()!
            : $"para todo {string.Join(" ", Quantificados.Select(Variavel.Nomear))}. {Corpo}";
}
