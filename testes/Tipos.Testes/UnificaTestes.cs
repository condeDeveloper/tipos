using Conde.Tipos;
using Xunit;

namespace Conde.Tipos.Testes;

public class UnificaTestes
{
    private static readonly Tipo A = new Variavel(0);
    private static readonly Tipo B = new Variavel(1);

    [Fact]
    public void DuasConstantesIguaisUnificamSemNadaANovo()
    {
        var r = Unifica.Unificar(Tipo.Inteiro, Tipo.Inteiro);

        Assert.True(r.Deu);
        Assert.Equal(0, r.Substituicao!.Quantidade);
    }

    [Fact]
    public void DuasConstantesDiferentesNaoUnificam()
    {
        var r = Unifica.Unificar(Tipo.Inteiro, Tipo.Logico);

        Assert.False(r.Deu);
        Assert.Equal("int nao e bool", r.Erro);
    }

    [Fact]
    public void UmaVariavelViraOQueEncontrar()
    {
        var r = Unifica.Unificar(A, Tipo.Inteiro);

        Assert.True(r.Deu);
        Assert.Equal(Tipo.Inteiro, r.Substituicao!.Aplicar(A));
    }

    [Fact]
    public void OLadoNaoImporta()
    {
        var daDireita = Unifica.Unificar(Tipo.Inteiro, A);

        Assert.True(daDireita.Deu);
        Assert.Equal(Tipo.Inteiro, daDireita.Substituicao!.Aplicar(A));
    }

    [Fact]
    public void AsSetasUnificamPorDentro()
    {
        var r = Unifica.Unificar(new Seta(A, Tipo.Logico), new Seta(Tipo.Inteiro, B));

        Assert.True(r.Deu);
        Assert.Equal(Tipo.Inteiro, r.Substituicao!.Aplicar(A));
        Assert.Equal(Tipo.Logico, r.Substituicao.Aplicar(B));
    }

    /// <summary>
    /// A segunda unificacao acontece JA COM o que a primeira descobriu aplicado.
    /// Sem isso, a mesma variavel dos dois lados receberia dois valores
    /// diferentes e o resultado seria incoerente.
    /// </summary>
    [Fact]
    public void AMesmaVariavelDosDoisLadosFicaCoerente()
    {
        var r = Unifica.Unificar(new Seta(A, A), new Seta(Tipo.Inteiro, B));

        Assert.True(r.Deu);
        Assert.Equal(Tipo.Inteiro, r.Substituicao!.Aplicar(A));
        Assert.Equal(Tipo.Inteiro, r.Substituicao.Aplicar(B));
    }

    [Fact]
    public void UmaSetaNaoUnificaComUmaConstante()
    {
        Assert.False(Unifica.Unificar(new Seta(A, B), Tipo.Inteiro).Deu);
        Assert.False(Unifica.Unificar(new Par(A, B), new Seta(A, B)).Deu);
    }

    /// <summary>
    /// O TESTE DE OCORRENCIA: "a" nao pode virar um tipo que contem "a". Sem
    /// ele, a unificacao produz um tipo infinito.
    /// </summary>
    [Fact]
    public void OTesteDeOcorrenciaRecusaOTipoInfinito()
    {
        var r = Unifica.Unificar(A, new Seta(A, B));

        Assert.False(r.Deu);
        Assert.Equal("ocorrencia: a aparece dentro de a -> b", r.Erro);
    }

    /// <summary>
    /// E sem ele a unificacao ACEITA, devolvendo uma substituicao que nunca
    /// estabiliza: o tipo ganha nos a cada aplicacao, para sempre.
    /// </summary>
    [Fact]
    public void SemOTesteASubstituicaoNuncaEstabiliza()
    {
        var r = Unifica.Unificar(A, new Seta(A, B), comTesteDeOcorrencia: false);
        Assert.True(r.Deu);

        var (tamanho, estabilizou) = Unifica.Crescimento(r.Substituicao!, A, 20);

        Assert.False(estabilizou);
        Assert.True(tamanho > 20, $"o tipo parou em {tamanho} nos");
    }

    /// <summary>
    /// Numa substituicao bem formada, aplicar duas vezes da o mesmo que aplicar
    /// uma. Esse ponto fixo e exatamente o que falta no caso de cima.
    /// </summary>
    [Fact]
    public void NumaSubstituicaoBemFormadaOCrescimentoPara()
    {
        var r = Unifica.Unificar(A, new Seta(Tipo.Inteiro, B));
        var (tamanho, estabilizou) = Unifica.Crescimento(r.Substituicao!, A);

        Assert.True(estabilizou);
        Assert.Equal(3, tamanho);
    }

    [Fact]
    public void UnificarUmaVariavelComElaMesmaNaoFazNada()
    {
        var r = Unifica.Unificar(A, new Variavel(0));

        Assert.True(r.Deu);
        Assert.Equal(0, r.Substituicao!.Quantidade);
    }

    [Fact]
    public void OsPassosSaoContados()
    {
        Assert.Equal(1, Unifica.Unificar(Tipo.Inteiro, Tipo.Inteiro).Passos);
        Assert.True(Unifica.Unificar(new Seta(A, B), new Seta(Tipo.Inteiro, Tipo.Logico)).Passos > 1);
    }
}
