namespace Conde.Tipos;

/// <summary>
/// A UNIFICACAO: achar a substituicao que torna dois tipos iguais.
///
/// Ela e o coracao da inferencia e cabe em meia pagina. Duas constantes unificam
/// se forem a mesma; duas setas unificam se os lados unificarem; e uma variavel
/// unifica com qualquer coisa, virando aquela coisa.
///
/// A ultima regra tem uma excecao, e e a parte mais interessante: a variavel
/// "a" nao pode virar um tipo que contem "a". Esse e o TESTE DE OCORRENCIA, e
/// sem ele a unificacao produz um tipo infinito e o algoritmo nao termina. A
/// medida liga e desliga esse teste para mostrar o que acontece.
/// </summary>
public static class Unifica
{
    /// <summary>O que a unificacao devolve: a substituicao, ou o motivo da falha.</summary>
    public sealed record Resultado(Substituicao? Substituicao, string? Erro, int Passos)
    {
        public bool Deu => Substituicao is not null;
    }

    public static Resultado Unificar(Tipo a, Tipo b, bool comTesteDeOcorrencia = true)
    {
        var passos = 0;
        var substituicao = Juntar(a, b, comTesteDeOcorrencia, ref passos, out var erro);
        return new Resultado(substituicao, erro, passos);
    }

    private static Substituicao? Juntar(Tipo a, Tipo b, bool teste, ref int passos, out string? erro)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        passos++;
        erro = null;

        switch (a, b)
        {
            case (Variavel va, _) when b is Variavel vb && va.Id == vb.Id:
                return Substituicao.Vazia;

            case (Variavel va, _):
                if (teste && b.Variaveis().Contains(va.Id))
                {
                    // O tipo infinito mora aqui. Sem este teste, unificar "a"
                    // com "a -> b" produz uma substituicao que, aplicada a si
                    // mesma, nunca para de crescer.
                    erro = $"ocorrencia: {va} aparece dentro de {b}";
                    return null;
                }
                return Substituicao.De(va.Id, b);

            case (_, Variavel):
                return Juntar(b, a, teste, ref passos, out erro);

            case (Constante ca, Constante cb):
                if (ca.Nome == cb.Nome) return Substituicao.Vazia;
                erro = $"{ca} nao e {cb}";
                return null;

            case (Seta sa, Seta sb):
                return Dois(sa.De, sb.De, sa.Para, sb.Para, teste, ref passos, out erro);

            case (Par pa, Par pb):
                return Dois(pa.Esquerda, pb.Esquerda, pa.Direita, pb.Direita, teste, ref passos, out erro);

            default:
                erro = $"{a} nao e {b}";
                return null;
        }
    }

    /// <summary>
    /// Unifica dois pares de tipos em sequencia.
    ///
    /// A segunda unificacao acontece JA COM o que a primeira descobriu aplicado.
    /// Fazer as duas independentes e depois juntar daria resultado errado sempre
    /// que a mesma variavel aparecesse dos dois lados.
    /// </summary>
    private static Substituicao? Dois(Tipo a1, Tipo b1, Tipo a2, Tipo b2, bool teste, ref int passos, out string? erro)
    {
        var primeira = Juntar(a1, b1, teste, ref passos, out erro);
        if (primeira is null) return null;

        var segunda = Juntar(primeira.Aplicar(a2), primeira.Aplicar(b2), teste, ref passos, out erro);
        if (segunda is null) return null;

        return primeira.Depois(segunda);
    }

    /// <summary>
    /// Aplica a substituicao repetidamente, para ver se ela ESTABILIZA.
    ///
    /// Num tipo bem formado, aplicar duas vezes da o mesmo que aplicar uma. Com
    /// o teste de ocorrencia desligado, a substituicao faz o tipo crescer a cada
    /// aplicacao, e e assim que o tipo infinito aparece em vez de ficar
    /// escondido num laco que nunca termina.
    /// </summary>
    public static (int Tamanho, bool Estabilizou) Crescimento(Substituicao substituicao, Tipo tipo, int vezes = 12)
    {
        ArgumentNullException.ThrowIfNull(substituicao);

        var atual = tipo;
        var anterior = tipo.Tamanho();

        for (var i = 0; i < vezes; i++)
        {
            atual = substituicao.Aplicar(atual);
            var agora = atual.Tamanho();

            if (agora == anterior) return (agora, true);
            anterior = agora;
        }

        return (anterior, false);
    }
}
