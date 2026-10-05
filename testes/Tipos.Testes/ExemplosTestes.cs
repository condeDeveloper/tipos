using Conde.Tipos;
using Xunit;

namespace Conde.Tipos.Testes;

/// <summary>
/// Cada exemplo existe para mostrar uma coisa so, e aqui cada um prova que
/// mostra mesmo aquilo.
/// </summary>
public class ExemplosTestes
{
    [Fact]
    public void AIdentidadeDevolveOQueRecebe()
    {
        foreach (var argumento in new Termo[] { Exemplos.Num(5), Exemplos.Log(true) })
        {
            var termo = Exemplos.Ap(Exemplos.Identidade(), argumento);
            Assert.Equal(Avaliador.Avaliar(argumento).Valor, Avaliador.Avaliar(termo).Valor);
        }
    }

    [Fact]
    public void AConstanteJogaOSegundoFora()
    {
        var termo = Exemplos.Ap(Exemplos.Constante(), Exemplos.Num(1), Exemplos.Log(true));
        Assert.Equal(new Inteiro(1), Avaliador.Avaliar(termo).Valor);
    }

    [Fact]
    public void AComposicaoEncaixaAsDuas()
    {
        var maisUm = Exemplos.Fun("n", new Soma(Exemplos.Var("n"), Exemplos.Num(1)));
        var maisDois = Exemplos.Fun("n", new Soma(Exemplos.Var("n"), Exemplos.Num(2)));

        var termo = Exemplos.Ap(Exemplos.Composicao(), maisUm, maisDois, Exemplos.Num(10));
        Assert.Equal(new Inteiro(13), Avaliador.Avaliar(termo).Valor);
    }

    /// <summary>
    /// O par de programas que e a demonstracao mais curta do repositorio: a
    /// mesma conta, uma com let e outra com aplicacao, com o MESMO resultado em
    /// execucao e destinos diferentes no sistema de tipos.
    /// </summary>
    [Fact]
    public void OParDeProgramasQueRodaIgualETipaDiferente()
    {
        var comLet = Exemplos.IdentidadeNosDoisTipos();
        var comAplicacao = Exemplos.IdentidadeNosDoisTiposSemLet();

        var esperado = new Dois(new Inteiro(1), new Logico(true));

        Assert.Equal(esperado, Avaliador.Avaliar(comLet).Valor);
        Assert.Equal(esperado, Avaliador.Avaliar(comAplicacao).Valor);

        Assert.True(Inferencia.Inferir(comLet).Tipou);
        Assert.False(Inferencia.Inferir(comAplicacao).Tipou);
    }

    /// <summary>
    /// O OMEGA e o unico exemplo em que recusar o programa foi mesmo a coisa
    /// certa: ele nao tipa e tambem nao termina.
    /// </summary>
    [Fact]
    public void OOmegaEhOUnicoCasoEmQueARecusaEstavaCerta()
    {
        Assert.False(Inferencia.Inferir(Exemplos.Omega()).Tipou);
        Assert.True(Avaliador.Avaliar(Exemplos.Omega(), 5_000).SemFim);
    }

    /// <summary>
    /// Os outros recusados se dividem em dois grupos: os que travam de verdade e
    /// os que rodariam bem. Os dois grupos existem, e o segundo e grande.
    /// </summary>
    [Fact]
    public void OsRecusadosSeDividemEmDoisGrupos()
    {
        var travam = 0;
        var rodam = 0;

        foreach (var (_, termo) in Exemplos.QueNaoTipam())
        {
            var r = Avaliador.Avaliar(termo, 5_000);
            if (r.Travou is not null) travam++;
            if (r.Terminou) rodam++;
        }

        Assert.True(travam > 0);
        Assert.True(rodam > 0);
    }

    /// <summary>
    /// A auto-aplicacao sozinha nao trava nem roda: ela e uma funcao, e ninguem
    /// chamou ela ainda. O que nao termina e aplicar ela a si mesma.
    /// </summary>
    [Fact]
    public void AAutoAplicacaoSozinhaEhSoUmaFuncao()
    {
        var r = Avaliador.Avaliar(Exemplos.AutoAplicacao());

        Assert.IsType<Fechamento>(r.Valor);
        Assert.Null(r.Travou);
    }

    /// <summary>
    /// A TORRE DE PARES roda e devolve uma arvore de identidades. O tipo dela e
    /// que explode, e o valor nao: sao 2^n fechamentos iguais.
    /// </summary>
    [Fact]
    public void ATorreDeParesRodaEDevolveUmaArvoreDeIdentidades()
    {
        var valor = Avaliador.Avaliar(Exemplos.TorreDePares(3)).Valor;

        var par = Assert.IsType<Dois>(valor);
        Assert.IsType<Dois>(par.Esquerda);
        Assert.Equal(par.Esquerda, par.Direita);
    }

    [Fact]
    public void OsAjudantesMontamOQueDizem()
    {
        Assert.Equal("fun x -> 1", Exemplos.Fun("x", Exemplos.Num(1)).ToString());
        Assert.Equal("((f 1) 2)", Exemplos.Ap(Exemplos.Var("f"), Exemplos.Num(1), Exemplos.Num(2)).ToString());
        Assert.Equal("f", Exemplos.Var("f").ToString());
        Assert.Equal("falso", Exemplos.Log(false).ToString());
    }

    [Fact]
    public void ATorreDeZeroAndaresEhSoAIdentidade()
    {
        var termo = Exemplos.TorreDePares(0);

        Assert.Equal("let p0 = fun x -> x in p0", termo.ToString());

        // o tipo e uma seta com os dois lados iguais, e o NUMERO da variavel
        // nao e parte da resposta: usar p0 instancia o esquema com uma variavel
        // fresca, entao sai "b -> b" e nao "a -> a"
        var seta = Assert.IsType<Seta>(Inferencia.Inferir(termo).Tipo);
        Assert.Equal(seta.De, seta.Para);
        Assert.IsType<Variavel>(seta.De);
    }
}
