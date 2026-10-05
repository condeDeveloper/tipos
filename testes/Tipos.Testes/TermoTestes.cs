using Conde.Tipos;
using Xunit;

namespace Conde.Tipos.Testes;

public class TermoTestes
{
    [Fact]
    public void OTamanhoContaOsNos()
    {
        Assert.Equal(1, Exemplos.Num(1).Tamanho());
        Assert.Equal(2, Exemplos.Identidade().Tamanho());
        Assert.Equal(3, Exemplos.Constante().Tamanho());
        Assert.Equal(8, Exemplos.Composicao().Tamanho());
    }

    /// <summary>
    /// A TORRE DE PARES cresce LINEAR no termo: quatro nos por andar. O tipo
    /// dela e que explode, e e esse contraste que o repositorio mede.
    /// </summary>
    [Fact]
    public void ATorreCresceLinearNoTermo()
    {
        var anterior = 0;

        for (var andares = 0; andares <= 10; andares++)
        {
            var tamanho = Exemplos.TorreDePares(andares).Tamanho();

            if (anterior > 0) Assert.Equal(4, tamanho - anterior);
            anterior = tamanho;
        }
    }

    [Fact]
    public void AEscritaDeCadaTermoEhLegivel()
    {
        Assert.Equal("fun x -> x", Exemplos.Identidade().ToString());
        Assert.Equal("fun x -> fun y -> x", Exemplos.Constante().ToString());
        Assert.Equal("fun x -> (x x)", Exemplos.AutoAplicacao().ToString());
        Assert.Equal("(1 + 2)", new Soma(Exemplos.Num(1), Exemplos.Num(2)).ToString());
        Assert.Equal("(1, verdadeiro)", new Dupla(Exemplos.Num(1), Exemplos.Log(true)).ToString());
        Assert.Equal("fst (1, 2)", new Projecao(true, new Dupla(Exemplos.Num(1), Exemplos.Num(2))).ToString());
    }

    /// <summary>
    /// A aplicacao em cadeia e a esquerda: f a b e ((f a) b), que e a unica
    /// leitura que faz sentido com funcoes de um argumento.
    /// </summary>
    [Fact]
    public void AAplicacaoEmCadeiaEhAEsquerda()
    {
        var termo = Exemplos.Ap(Exemplos.Var("f"), Exemplos.Num(1), Exemplos.Num(2));

        Assert.Equal("((f 1) 2)", termo.ToString());
        Assert.IsType<Aplicacao>(termo);
        Assert.IsType<Aplicacao>(((Aplicacao)termo).Alvo);
    }

    /// <summary>
    /// O let e a aplicacao equivalente tem o MESMO comportamento em execucao, e
    /// e por isso que a diferenca de tipagem entre os dois surpreende.
    /// </summary>
    [Fact]
    public void OLetEAAplicacaoEquivalenteRodamIgual()
    {
        var comLet = new Deixe("x", Exemplos.Num(7), new Soma(Exemplos.Var("x"), Exemplos.Var("x")));
        var comAplicacao = Exemplos.Ap(
            Exemplos.Fun("x", new Soma(Exemplos.Var("x"), Exemplos.Var("x"))), Exemplos.Num(7));

        Assert.Equal(Avaliador.Avaliar(comLet).Valor, Avaliador.Avaliar(comAplicacao).Valor);
        Assert.Equal(new Inteiro(14), Avaliador.Avaliar(comLet).Valor);
    }

    [Fact]
    public void OsExemplosEstaoTodosAli()
    {
        Assert.Equal(9, Exemplos.QueTipam().Count());
        Assert.Equal(8, Exemplos.QueNaoTipam().Count());

        foreach (var (nome, termo) in Exemplos.QueTipam().Concat(Exemplos.QueNaoTipam()))
        {
            Assert.False(string.IsNullOrWhiteSpace(nome));
            Assert.True(termo.Tamanho() > 0);
        }
    }
}
