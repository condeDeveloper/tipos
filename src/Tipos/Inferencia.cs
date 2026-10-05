namespace Conde.Tipos;

/// <summary>
/// O ALGORITMO W, de Damas e Milner, 1982: descobrir o tipo de um programa sem
/// nenhuma anotacao.
///
/// Ele percorre o termo uma vez, inventa uma variavel de tipo para cada lacuna e
/// usa a unificacao para preencher as lacunas conforme as pecas se encaixam. No
/// fim, o que sobrou sem preencher e polimorfismo.
///
/// O que ele devolve e o tipo PRINCIPAL: o mais geral de todos os tipos que
/// aquele termo tem. Nao um tipo que serve, o mais geral que serve, e e por isso
/// que programa nenhum precisa de anotacao.
///
/// A generalizacao acontece so no LET, e o repositorio inteiro gira em torno
/// disso: a mesma conta com let ou com aplicacao roda igual e tipa diferente.
/// </summary>
public sealed class Inferencia
{
    private int proxima;

    /// <summary>Quantas unificacoes a inferencia pediu, que e a medida de custo.</summary>
    public int Unificacoes { get; private set; }

    /// <summary>Quantos passos a unificacao gastou somados, que e a medida fina.</summary>
    public int PassosDeUnificacao { get; private set; }

    /// <summary>Se o let generaliza. Desligar isso e o sistema monomorfico.</summary>
    public bool ComPolimorfismo { get; init; } = true;

    public bool ComTesteDeOcorrencia { get; init; } = true;

    public sealed record Resultado(Tipo? Tipo, string? Erro, int Unificacoes, int PassosDeUnificacao)
    {
        public bool Tipou => Tipo is not null;

        public override string ToString() => Tipou ? Tipo!.ToString()! : $"erro: {Erro}";
    }

    public static Resultado Inferir(Termo termo, bool comPolimorfismo = true, bool comTesteDeOcorrencia = true)
    {
        var inferencia = new Inferencia
        {
            ComPolimorfismo = comPolimorfismo,
            ComTesteDeOcorrencia = comTesteDeOcorrencia,
        };

        var (substituicao, tipo, erro) = inferencia.Passo(new Ambiente(), termo);

        return new Resultado(erro is null ? substituicao!.Aplicar(tipo!) : null, erro,
                             inferencia.Unificacoes, inferencia.PassosDeUnificacao);
    }

    private Tipo Fresca() => new Variavel(proxima++);

    /// <summary>
    /// INSTANCIAR um esquema: trocar cada variavel quantificada por uma fresca.
    ///
    /// E aqui que o polimorfismo acontece de verdade. Cada uso de um nome
    /// polimorfico ganha variaveis novas, entao usar a identidade num inteiro
    /// nao prende ela a inteiro para o proximo uso.
    /// </summary>
    private Tipo Instanciar(Esquema esquema)
    {
        if (esquema.Quantificados.Count == 0) return esquema.Corpo;

        var troca = new Substituicao();
        foreach (var ligada in esquema.Quantificados)
            troca = troca.Depois(Substituicao.De(ligada, Fresca()));

        return troca.Aplicar(esquema.Corpo);
    }

    /// <summary>
    /// GENERALIZAR um tipo: quantificar as variaveis que nao aparecem no
    /// ambiente.
    ///
    /// As que aparecem no ambiente podem ser decididas depois por outra parte do
    /// programa, entao quantificar elas prometeria uma generalidade que nao
    /// existe. Esse e o cuidado que faz a diferenca entre um sistema correto e
    /// um que aceita programas que travam.
    /// </summary>
    private static Esquema Generalizar(Ambiente ambiente, Tipo tipo)
    {
        var doAmbiente = ambiente.VariaveisLivres();
        var soltas = tipo.Variaveis().Distinct().Where(v => !doAmbiente.Contains(v)).ToList();

        return new Esquema(soltas, tipo);
    }

    private (Substituicao? Substituicao, Tipo? Tipo, string? Erro) Passo(Ambiente ambiente, Termo termo)
    {
        switch (termo)
        {
            case Numero:
                return (Substituicao.Vazia, Tipo.Inteiro, null);

            case Booleano:
                return (Substituicao.Vazia, Tipo.Logico, null);

            case Nome n:
                var esquema = ambiente.Buscar(n.Id);
                if (esquema is null) return (null, null, $"nome livre: {n.Id}");
                return (Substituicao.Vazia, Instanciar(esquema), null);

            case Funcao f:
            {
                var doParametro = Fresca();
                var dentro = ambiente.Com(f.Parametro, Esquema.Simples(doParametro));

                var (s, doCorpo, erro) = Passo(dentro, f.Corpo);
                if (erro is not null) return (null, null, erro);

                return (s, new Seta(s!.Aplicar(doParametro), doCorpo!), null);
            }

            case Aplicacao a:
            {
                var (s1, doAlvo, erro1) = Passo(ambiente, a.Alvo);
                if (erro1 is not null) return (null, null, erro1);

                var (s2, doArgumento, erro2) = Passo(ambiente.Aplicar(s1!), a.Argumento);
                if (erro2 is not null) return (null, null, erro2);

                var resposta = Fresca();
                var (s3, erro3) = Juntar(s2!.Aplicar(doAlvo!), new Seta(doArgumento!, resposta));
                if (erro3 is not null) return (null, null, erro3);

                return (s1!.Depois(s2).Depois(s3!), s3!.Aplicar(resposta), null);
            }

            case Deixe d:
            {
                var (s1, doValor, erro1) = Passo(ambiente, d.Valor);
                if (erro1 is not null) return (null, null, erro1);

                var depois = ambiente.Aplicar(s1!);

                // A UNICA linha que separa o polimorfico do monomorfico. Com
                // ela, cada uso do nome ganha variaveis frescas; sem ela, o
                // primeiro uso decide o tipo para todos os outros.
                var esquemaDoValor = ComPolimorfismo
                    ? Generalizar(depois, s1!.Aplicar(doValor!))
                    : Esquema.Simples(s1!.Aplicar(doValor!));

                var (s2, doCorpo, erro2) = Passo(depois.Com(d.Id, esquemaDoValor), d.Corpo);
                if (erro2 is not null) return (null, null, erro2);

                return (s1.Depois(s2!), doCorpo, null);
            }

            case Se s:
            {
                var (s1, daCondicao, erro1) = Passo(ambiente, s.Condicao);
                if (erro1 is not null) return (null, null, erro1);

                var (sl, erroL) = Juntar(daCondicao!, Tipo.Logico);
                if (erroL is not null) return (null, null, erroL);

                var juntas = s1!.Depois(sl!);

                var (s2, doEntao, erro2) = Passo(ambiente.Aplicar(juntas), s.Entao);
                if (erro2 is not null) return (null, null, erro2);

                juntas = juntas.Depois(s2!);

                var (s3, doSenao, erro3) = Passo(ambiente.Aplicar(juntas), s.Senao);
                if (erro3 is not null) return (null, null, erro3);

                juntas = juntas.Depois(s3!);

                var (s4, erro4) = Juntar(juntas.Aplicar(doEntao!), juntas.Aplicar(doSenao!));
                if (erro4 is not null) return (null, null, erro4);

                juntas = juntas.Depois(s4!);
                return (juntas, juntas.Aplicar(doEntao!), null);
            }

            case Dupla p:
            {
                var (s1, daEsquerda, erro1) = Passo(ambiente, p.Esquerda);
                if (erro1 is not null) return (null, null, erro1);

                var (s2, daDireita, erro2) = Passo(ambiente.Aplicar(s1!), p.Direita);
                if (erro2 is not null) return (null, null, erro2);

                var juntas = s1!.Depois(s2!);
                return (juntas, new Par(juntas.Aplicar(daEsquerda!), juntas.Aplicar(daDireita!)), null);
            }

            case Projecao j:
            {
                var (s1, doAlvo, erro1) = Passo(ambiente, j.Alvo);
                if (erro1 is not null) return (null, null, erro1);

                Tipo esquerda = Fresca(), direita = Fresca();
                var (s2, erro2) = Juntar(doAlvo!, new Par(esquerda, direita));
                if (erro2 is not null) return (null, null, erro2);

                var juntas = s1!.Depois(s2!);
                return (juntas, juntas.Aplicar(j.Primeiro ? esquerda : direita), null);
            }

            case Soma m:
            {
                var (s1, daEsquerda, erro1) = Passo(ambiente, m.Esquerda);
                if (erro1 is not null) return (null, null, erro1);

                var (se, erroE) = Juntar(daEsquerda!, Tipo.Inteiro);
                if (erroE is not null) return (null, null, erroE);

                var juntas = s1!.Depois(se!);

                var (s2, daDireita, erro2) = Passo(ambiente.Aplicar(juntas), m.Direita);
                if (erro2 is not null) return (null, null, erro2);

                juntas = juntas.Depois(s2!);

                var (sd, erroD) = Juntar(juntas.Aplicar(daDireita!), Tipo.Inteiro);
                if (erroD is not null) return (null, null, erroD);

                return (juntas.Depois(sd!), Tipo.Inteiro, null);
            }

            default:
                return (null, null, $"termo desconhecido: {termo}");
        }
    }

    private (Substituicao? Substituicao, string? Erro) Juntar(Tipo a, Tipo b)
    {
        Unificacoes++;
        var resultado = Unifica.Unificar(a, b, ComTesteDeOcorrencia);
        PassosDeUnificacao += resultado.Passos;

        return (resultado.Substituicao, resultado.Erro);
    }
}
