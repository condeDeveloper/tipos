using Conde.Tipos;
using Xunit;

namespace Conde.Tipos.Testes;

public class AvaliadorTestes
{
    [Fact]
    public void OsLiteraisAvaliamParaEstesMesmos()
    {
        Assert.Equal(new Inteiro(7), Avaliador.Avaliar(Exemplos.Num(7)).Valor);
        Assert.Equal(new Logico(true), Avaliador.Avaliar(Exemplos.Log(true)).Valor);
    }

    [Fact]
    public void AAplicacaoSubstituiOArgumento()
    {
        var termo = Exemplos.Ap(Exemplos.Fun("x", new Soma(Exemplos.Var("x"), Exemplos.Num(1))), Exemplos.Num(41));
        Assert.Equal(new Inteiro(42), Avaliador.Avaliar(termo).Valor);
    }

    /// <summary>
    /// O fechamento guarda a memoria de onde a funcao foi CRIADA, e nao de onde
    /// ela e chamada. Sem isso, o "x" de dentro veria o "x" de fora e o programa
    /// daria outra resposta.
    /// </summary>
    [Fact]
    public void OFechamentoGuardaAMemoriaDeOndeFoiCriado()
    {
        // let x = 1 in let f = fun y -> x in let x = 2 in f 0
        var termo = new Deixe("x", Exemplos.Num(1),
            new Deixe("f", Exemplos.Fun("y", Exemplos.Var("x")),
                new Deixe("x", Exemplos.Num(2),
                    Exemplos.Ap(Exemplos.Var("f"), Exemplos.Num(0)))));

        Assert.Equal(new Inteiro(1), Avaliador.Avaliar(termo).Valor);
    }

    [Fact]
    public void OSeSoAvaliaORamoEscolhido()
    {
        var termo = new Se(Exemplos.Log(true), Exemplos.Num(1), new Soma(Exemplos.Num(1), Exemplos.Log(true)));

        Assert.Equal(new Inteiro(1), Avaliador.Avaliar(termo).Valor);
        Assert.Null(Avaliador.Avaliar(termo).Travou);
    }

    [Fact]
    public void AsProjecoesPegamOLadoCerto()
    {
        var par = new Dupla(Exemplos.Num(1), Exemplos.Log(false));

        Assert.Equal(new Inteiro(1), Avaliador.Avaliar(new Projecao(true, par)).Valor);
        Assert.Equal(new Logico(false), Avaliador.Avaliar(new Projecao(false, par)).Valor);
    }

    /// <summary>
    /// As cinco maneiras de TRAVAR, uma por uma. "Travar" nao e metafora: e esta
    /// lista, e e sobre ela que o sistema de tipos faz a promessa dele.
    /// </summary>
    [Fact]
    public void AsCincoManeirasDeTravar()
    {
        Assert.Contains("nao e funcao", Avaliador.Avaliar(Exemplos.AplicarNumero()).Travou!, StringComparison.Ordinal);
        Assert.Contains("nao e numero", Avaliador.Avaliar(Exemplos.SomaComLogico()).Travou!, StringComparison.Ordinal);
        Assert.Contains("nao e logico", Avaliador.Avaliar(Exemplos.DesvioPorNumero()).Travou!, StringComparison.Ordinal);
        Assert.Contains("nao e par",
                        Avaliador.Avaliar(new Projecao(true, Exemplos.Num(1))).Travou!, StringComparison.Ordinal);
        Assert.Contains("nome livre", Avaliador.Avaliar(Exemplos.Var("x")).Travou!, StringComparison.Ordinal);
    }

    /// <summary>
    /// O OMEGA nao termina, e o limite de passos pega isso em vez de travar a
    /// maquina. Ele e o unico exemplo do repositorio em que recusar o programa
    /// foi mesmo a coisa certa.
    /// </summary>
    [Fact]
    public void OOmegaNaoTermina()
    {
        var r = Avaliador.Avaliar(Exemplos.Omega(), 5_000);

        Assert.True(r.SemFim);
        Assert.Null(r.Valor);
        Assert.Null(r.Travou);
    }

    /// <summary>
    /// O programa que NAO tipa e roda bem: o ramo com erro nunca e executado. O
    /// sistema de tipos olha os dois caminhos, e o programa so anda por um.
    /// </summary>
    [Fact]
    public void ORamoRuimQueNuncaRodaNaoTipaERodaBem()
    {
        var termo = Exemplos.RamoRuimQueNuncaRoda();

        Assert.False(Inferencia.Inferir(termo).Tipou);
        Assert.Equal(new Inteiro(1), Avaliador.Avaliar(termo).Valor);
    }

    /// <summary>
    /// E este tambem nao tipa e roda bem, pelo outro motivo: o let generaliza e
    /// a aplicacao nao, entao a mesma conta escrita de duas formas tem destinos
    /// diferentes no sistema de tipos e o mesmo destino em execucao.
    /// </summary>
    [Fact]
    public void OIdNosDoisTiposSemLetNaoTipaERodaBem()
    {
        var termo = Exemplos.IdentidadeNosDoisTiposSemLet();

        Assert.False(Inferencia.Inferir(termo).Tipou);
        Assert.Equal(new Dois(new Inteiro(1), new Logico(true)), Avaliador.Avaliar(termo).Valor);
    }

    [Fact]
    public void OsPassosSaoContados()
    {
        Assert.Equal(1, Avaliador.Avaliar(Exemplos.Num(1)).Passos);
        Assert.True(Avaliador.Avaliar(Exemplos.IdentidadeNosDoisTipos()).Passos > 5);
    }

    [Fact]
    public void AEscritaDosValoresEhLegivel()
    {
        Assert.Equal("7", new Inteiro(7).ToString());
        Assert.Equal("verdadeiro", new Logico(true).ToString());
        Assert.Equal("(1, falso)", new Dois(new Inteiro(1), new Logico(false)).ToString());
        Assert.Equal("<fun x>", Avaliador.Avaliar(Exemplos.Identidade()).Valor!.ToString());
    }
}
