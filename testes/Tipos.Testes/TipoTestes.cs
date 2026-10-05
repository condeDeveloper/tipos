using Conde.Tipos;
using Xunit;

namespace Conde.Tipos.Testes;

public class TipoTestes
{
    [Fact]
    public void AsVariaveisSaoAsQueAparecem()
    {
        Tipo a = new Variavel(0), b = new Variavel(1);

        Assert.Equal(new[] { 0 }, a.Variaveis());
        Assert.Equal(new[] { 0, 1 }, new Seta(a, b).Variaveis());
        Assert.Equal(new[] { 0, 1, 0 }, new Par(new Seta(a, b), a).Variaveis());
        Assert.Empty(Tipo.Inteiro.Variaveis());
    }

    [Fact]
    public void OTamanhoContaOsNos()
    {
        Tipo a = new Variavel(0);

        Assert.Equal(1, a.Tamanho());
        Assert.Equal(1, Tipo.Inteiro.Tamanho());
        Assert.Equal(3, new Seta(a, a).Tamanho());
        Assert.Equal(5, new Seta(new Seta(a, a), a).Tamanho());
    }

    /// <summary>
    /// A seta associa a direita, entao a -> b -> c e a -> (b -> c), e so o lado
    /// esquerdo ganha parenteses na escrita.
    /// </summary>
    [Fact]
    public void ASetaAssociaADireitaNaEscrita()
    {
        Tipo a = new Variavel(0), b = new Variavel(1), c = new Variavel(2);

        Assert.Equal("a -> b -> c", new Seta(a, new Seta(b, c)).ToString());
        Assert.Equal("(a -> b) -> c", new Seta(new Seta(a, b), c).ToString());
    }

    [Fact]
    public void AsVariaveisTemNomeLegivel()
    {
        Assert.Equal("a", Variavel.Nomear(0));
        Assert.Equal("z", Variavel.Nomear(25));
        Assert.Equal("a1", Variavel.Nomear(26));
        Assert.Equal("b2", Variavel.Nomear(53));
    }

    /// <summary>
    /// A diferenca entre "a -> a" e "para todo a. a -> a" e a diferenca entre
    /// uma funcao cujo tipo ainda sera decidido e uma que serve para todos os
    /// tipos.
    /// </summary>
    [Fact]
    public void OEsquemaMostraOQueEstaQuantificado()
    {
        Tipo a = new Variavel(0);
        var seta = new Seta(a, a);

        Assert.Equal("a -> a", Esquema.Simples(seta).ToString());
        Assert.Equal("para todo a. a -> a", new Esquema(new[] { 0 }, seta).ToString());
    }

    [Fact]
    public void OsTiposSaoComparadosPorEstrutura()
    {
        Tipo a = new Variavel(0);

        Assert.Equal(new Seta(a, Tipo.Inteiro), new Seta(new Variavel(0), Tipo.Inteiro));
        Assert.NotEqual(new Seta(a, Tipo.Inteiro), new Seta(a, Tipo.Logico));
        Assert.Equal(Tipo.Inteiro, new Constante("int"));
    }
}
