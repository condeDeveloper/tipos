namespace Conde.Tipos;

/// <summary>
/// Um TERMO: a linguagem inteira deste repositorio.
///
/// Ela e minuscula de proposito. Numero, logico, nome, funcao de um argumento,
/// aplicacao, let, se, par e as duas projecoes, mais a soma. Isso basta para
/// escrever tudo que interessa sobre tipos, e nao tem nada que distraia: nao ha
/// classe, modulo, excecao nem efeito.
///
/// O que ela TEM de proposito e o let separado da aplicacao. Escrever
/// "let f = e1 in e2" e "(fun f -> e2) e1" roda exatamente igual, e tipa
/// diferente: so o let generaliza. A medida mostra o tamanho dessa diferenca.
/// </summary>
public abstract record Termo
{
    /// <summary>O tamanho do termo em nos, para as medidas de custo.</summary>
    public int Tamanho() => this switch
    {
        Funcao f => 1 + f.Corpo.Tamanho(),
        Aplicacao a => 1 + a.Alvo.Tamanho() + a.Argumento.Tamanho(),
        Deixe d => 1 + d.Valor.Tamanho() + d.Corpo.Tamanho(),
        Se s => 1 + s.Condicao.Tamanho() + s.Entao.Tamanho() + s.Senao.Tamanho(),
        Dupla p => 1 + p.Esquerda.Tamanho() + p.Direita.Tamanho(),
        Projecao j => 1 + j.Alvo.Tamanho(),
        Soma m => 1 + m.Esquerda.Tamanho() + m.Direita.Tamanho(),
        _ => 1,
    };
}

public sealed record Numero(int Valor) : Termo
{
    public override string ToString() => Valor.ToString();
}

public sealed record Booleano(bool Valor) : Termo
{
    public override string ToString() => Valor ? "verdadeiro" : "falso";
}

public sealed record Nome(string Id) : Termo
{
    public override string ToString() => Id;
}

public sealed record Funcao(string Parametro, Termo Corpo) : Termo
{
    public override string ToString() => $"fun {Parametro} -> {Corpo}";
}

public sealed record Aplicacao(Termo Alvo, Termo Argumento) : Termo
{
    public override string ToString() => $"({Alvo} {Argumento})";
}

/// <summary>
/// O LET, que e a peca central do repositorio.
///
/// Ele roda igual a uma aplicacao e tipa diferente: o tipo do valor e
/// GENERALIZADO antes de entrar no ambiente, entao cada uso dele ganha
/// variaveis frescas. E so por causa disso que "let id = fun x -> x in
/// (id 1, id verdadeiro)" tipa.
/// </summary>
public sealed record Deixe(string Id, Termo Valor, Termo Corpo) : Termo
{
    public override string ToString() => $"let {Id} = {Valor} in {Corpo}";
}

public sealed record Se(Termo Condicao, Termo Entao, Termo Senao) : Termo
{
    public override string ToString() => $"se {Condicao} entao {Entao} senao {Senao}";
}

public sealed record Dupla(Termo Esquerda, Termo Direita) : Termo
{
    public override string ToString() => $"({Esquerda}, {Direita})";
}

public sealed record Projecao(bool Primeiro, Termo Alvo) : Termo
{
    public override string ToString() => $"{(Primeiro ? "fst" : "snd")} {Alvo}";
}

public sealed record Soma(Termo Esquerda, Termo Direita) : Termo
{
    public override string ToString() => $"({Esquerda} + {Direita})";
}
