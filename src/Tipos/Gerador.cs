namespace Conde.Tipos;

/// <summary>
/// Um gerador de programas ALEATORIOS, para a medida de solidez.
///
/// A promessa do sistema de tipos e sobre TODOS os programas, entao conferir
/// oito exemplos escolhidos a mao nao mede quase nada. O que mede e gerar
/// milhares de programas sem nenhum cuidado, perguntar quais tipam e rodar
/// todos: nenhum dos tipados pode travar.
///
/// O sorteio e de um gerador congruente proprio, e nao do sorteio da
/// biblioteca. Isso nao e desconfianca: a documentacao do sorteio de biblioteca
/// nao promete a mesma sequencia entre versoes nem entre sistemas, e a
/// integracao continua roda em tres sistemas. Com o gerador escrito aqui, a
/// semente 7 produz exatamente os mesmos programas em todos.
/// </summary>
public sealed class Gerador
{
    private ulong estado;

    public Gerador(ulong semente) => estado = semente == 0 ? 1 : semente;

    /// <summary>
    /// O congruente linear de Numerical Recipes. Multiplicador e incremento
    /// escritos por extenso: eles sao a definicao da sequencia.
    /// </summary>
    public uint Proximo()
    {
        estado = estado * 1664525UL + 1013904223UL;
        return (uint)(estado >> 16);
    }

    public int Ate(int limite) => (int)(Proximo() % (uint)limite);

    /// <summary>
    /// Um termo qualquer, com a profundidade limitada.
    ///
    /// Nada aqui tenta produzir programa que tipa. A maioria nao tipa, e e
    /// exatamente isso que faz a medida valer: a amostra nao foi escolhida para
    /// o sistema se dar bem nela.
    /// </summary>
    public Termo Qualquer(int profundidade, IReadOnlyList<string>? visiveis = null)
    {
        visiveis ??= Array.Empty<string>();

        if (profundidade <= 0 || Ate(100) < 25)
        {
            return Ate(3) switch
            {
                0 => new Numero(Ate(10)),
                1 => new Booleano(Ate(2) == 0),
                _ => visiveis.Count > 0 ? new Nome(visiveis[Ate(visiveis.Count)]) : new Numero(Ate(10)),
            };
        }

        var nome = $"v{profundidade}";
        var comNome = visiveis.Append(nome).ToList();

        return Ate(8) switch
        {
            0 => new Funcao(nome, Qualquer(profundidade - 1, comNome)),
            1 => new Aplicacao(Qualquer(profundidade - 1, visiveis), Qualquer(profundidade - 1, visiveis)),
            2 => new Deixe(nome, Qualquer(profundidade - 1, visiveis), Qualquer(profundidade - 1, comNome)),
            3 => new Se(Qualquer(profundidade - 1, visiveis), Qualquer(profundidade - 1, visiveis),
                        Qualquer(profundidade - 1, visiveis)),
            4 => new Dupla(Qualquer(profundidade - 1, visiveis), Qualquer(profundidade - 1, visiveis)),
            5 => new Projecao(Ate(2) == 0, Qualquer(profundidade - 1, visiveis)),
            6 => new Soma(Qualquer(profundidade - 1, visiveis), Qualquer(profundidade - 1, visiveis)),
            _ => new Funcao(nome, Qualquer(profundidade - 1, comNome)),
        };
    }

    /// <summary>O que a medida de solidez conta.</summary>
    public sealed record Contagem(int Total, int Tiparam, int TiparamETravaram, int TiparamESemFim,
                                  int NaoTiparam, int NaoTiparamERodaram)
    {
        /// <summary>
        /// A fracao de programas recusados que teriam rodado bem. E o preco do
        /// sistema de tipos, e ele nao e pequeno.
        /// </summary>
        public double PrecoDaRecusa => NaoTiparam == 0 ? 0 : (double)NaoTiparamERodaram / NaoTiparam;
    }

    /// <summary>
    /// A MEDIDA DE SOLIDEZ: gera, tipa, roda, conta.
    ///
    /// O numero que interessa e TiparamETravaram, e ele tem que ser zero. O
    /// segundo numero que interessa e NaoTiparamERodaram, e ele e grande: o
    /// sistema recusa muito programa bom.
    /// </summary>
    public static Contagem Medir(ulong semente, int quantos, int profundidade = 4, bool comPolimorfismo = true)
    {
        var gerador = new Gerador(semente);
        int tiparam = 0, travaram = 0, semFim = 0, naoTiparam = 0, rodaram = 0;

        for (var i = 0; i < quantos; i++)
        {
            var termo = gerador.Qualquer(profundidade);
            var tipo = Inferencia.Inferir(termo, comPolimorfismo);
            var rodada = Avaliador.Avaliar(termo, 20_000);

            if (tipo.Tipou)
            {
                tiparam++;
                if (rodada.Travou is not null) travaram++;
                if (rodada.SemFim) semFim++;
            }
            else
            {
                naoTiparam++;
                if (rodada.Terminou) rodaram++;
            }
        }

        return new Contagem(quantos, tiparam, travaram, semFim, naoTiparam, rodaram);
    }
}
