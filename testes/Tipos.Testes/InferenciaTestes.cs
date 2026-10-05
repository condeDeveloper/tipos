using Conde.Tipos;
using Xunit;

namespace Conde.Tipos.Testes;

public class InferenciaTestes
{
    [Fact]
    public void OsLiteraisTemOTipoQueTem()
    {
        Assert.Equal(Tipo.Inteiro, Inferencia.Inferir(Exemplos.Num(1)).Tipo);
        Assert.Equal(Tipo.Logico, Inferencia.Inferir(Exemplos.Log(true)).Tipo);
    }

    /// <summary>
    /// O tipo da identidade e "a -> a", com a mesma variavel nos dois lados. Uma
    /// variavel diferente de cada lado seria um tipo mais geral e ERRADO: ele
    /// prometeria que a funcao pode devolver qualquer coisa.
    /// </summary>
    [Fact]
    public void AIdentidadeTemOMesmoTipoNosDoisLados()
    {
        var tipo = Inferencia.Inferir(Exemplos.Identidade()).Tipo;

        var seta = Assert.IsType<Seta>(tipo);
        Assert.Equal(seta.De, seta.Para);
        Assert.Equal("a -> a", tipo!.ToString());
    }

    [Fact]
    public void AConstanteJogaOSegundoArgumentoFora()
    {
        var tipo = Inferencia.Inferir(Exemplos.Constante()).Tipo;

        var seta = Assert.IsType<Seta>(tipo);
        var dentro = Assert.IsType<Seta>(seta.Para);

        Assert.Equal(seta.De, dentro.Para);
        Assert.NotEqual(seta.De, dentro.De);
    }

    [Fact]
    public void AComposicaoEncaixaAsDuasFuncoes()
    {
        var tipo = Inferencia.Inferir(Exemplos.Composicao()).Tipo;
        Assert.Equal("(d -> e) -> (c -> d) -> c -> e", tipo!.ToString());
    }

    [Fact]
    public void TodosOsExemplosQueDevemTiparTipam()
    {
        foreach (var (nome, termo) in Exemplos.QueTipam())
        {
            var r = Inferencia.Inferir(termo);
            Assert.True(r.Tipou, $"{nome} nao tipou: {r.Erro}");
        }
    }

    [Fact]
    public void TodosOsExemplosQueDevemFalharFalham()
    {
        foreach (var (nome, termo) in Exemplos.QueNaoTipam())
        {
            var r = Inferencia.Inferir(termo);
            Assert.False(r.Tipou, $"{nome} tipou como {r.Tipo}, e nao devia");
            Assert.False(string.IsNullOrWhiteSpace(r.Erro));
        }
    }

    /// <summary>
    /// O let-polimorfismo em uma linha: a mesma identidade usada num inteiro e
    /// num logico.
    /// </summary>
    [Fact]
    public void OLetPolimorfismoPermiteDoisTiposNoMesmoNome()
    {
        var termo = Exemplos.IdentidadeNosDoisTipos();

        Assert.Equal(new Par(Tipo.Inteiro, Tipo.Logico), Inferencia.Inferir(termo).Tipo);
        Assert.False(Inferencia.Inferir(termo, comPolimorfismo: false).Tipou);
    }

    /// <summary>
    /// E a mesma conta escrita com aplicacao em vez de let NAO tipa nos dois
    /// sistemas: so o let generaliza. As duas formas rodam exatamente igual.
    /// </summary>
    [Fact]
    public void SoOLetGeneraliza()
    {
        var comLet = Exemplos.IdentidadeNosDoisTipos();
        var comAplicacao = Exemplos.IdentidadeNosDoisTiposSemLet();

        Assert.True(Inferencia.Inferir(comLet).Tipou);
        Assert.False(Inferencia.Inferir(comAplicacao).Tipou);

        Assert.Equal(Avaliador.Avaliar(comLet).Valor, Avaliador.Avaliar(comAplicacao).Valor);
    }

    /// <summary>
    /// A generalizacao nao pode quantificar variavel que ainda aparece no
    /// ambiente. Aqui o "x" de fora prende o tipo, e generalizar ele deixaria o
    /// sistema aceitar um programa que trava.
    /// </summary>
    [Fact]
    public void AGeneralizacaoNaoQuantificaOQueOAmbienteAindaUsa()
    {
        // fun x -> let y = x in (y 1, y verdadeiro)
        var termo = Exemplos.Fun("x",
            new Deixe("y", Exemplos.Var("x"),
                new Dupla(Exemplos.Ap(Exemplos.Var("y"), Exemplos.Num(1)),
                          Exemplos.Ap(Exemplos.Var("y"), Exemplos.Log(true)))));

        Assert.False(Inferencia.Inferir(termo).Tipou);
    }

    [Fact]
    public void NomeLivreEhRecusado()
    {
        var r = Inferencia.Inferir(Exemplos.Var("nunca_definido"));

        Assert.False(r.Tipou);
        Assert.Contains("nome livre", r.Erro!, StringComparison.Ordinal);
    }

    /// <summary>
    /// O teste de ocorrencia desligado faz o algoritmo ACEITAR a auto-aplicacao
    /// e devolver um tipo. O tipo esta errado: ele e a primeira volta de uma
    /// coisa que nao termina.
    /// </summary>
    [Fact]
    public void SemOTesteDeOcorrenciaAAutoAplicacaoEhAceita()
    {
        Assert.False(Inferencia.Inferir(Exemplos.AutoAplicacao()).Tipou);
        Assert.True(Inferencia.Inferir(Exemplos.AutoAplicacao(), comTesteDeOcorrencia: false).Tipou);
    }

    /// <summary>
    /// O ACHADO do custo: o termo cresce LINEAR e o tipo cresce DOBRANDO.
    ///
    /// E a coluna das unificacoes fica em ZERO: o algoritmo nao esta trabalhando
    /// duro, ele so esta montando um tipo gigante. O pior caso exponencial da
    /// inferencia esta no tamanho da resposta, nao na dificuldade do problema.
    /// </summary>
    [Fact]
    public void OTipoDaTorreDobraACadaAndar()
    {
        var anterior = 0;

        for (var andares = 0; andares <= 12; andares++)
        {
            var r = Inferencia.Inferir(Exemplos.TorreDePares(andares));

            Assert.True(r.Tipou);
            Assert.Equal(0, r.Unificacoes);
            // o tipo do andar zero ja tem tres nos, porque a identidade e uma
            // seta; cada andar seguinte dobra o que tinha e soma o no do par
            Assert.Equal(Math.Pow(2, andares + 2) - 1, r.Tipo!.Tamanho());

            if (anterior > 0) Assert.Equal(anterior * 2 + 1, r.Tipo.Tamanho());
            anterior = r.Tipo.Tamanho();
        }
    }

    [Fact]
    public void AsUnificacoesSaoContadas()
    {
        Assert.Equal(0, Inferencia.Inferir(Exemplos.Identidade()).Unificacoes);
        Assert.True(Inferencia.Inferir(Exemplos.Composicao()).Unificacoes > 0);
        Assert.True(Inferencia.Inferir(Exemplos.Composicao()).PassosDeUnificacao > 0);
    }
}
