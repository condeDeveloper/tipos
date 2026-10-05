using Conde.Tipos;
using Xunit;

namespace Conde.Tipos.Testes;

/// <summary>
/// A SOLIDEZ: programa bem tipado nao trava, contado e nao suposto.
///
/// A promessa do sistema de tipos e sobre todos os programas, entao conferir
/// oito exemplos escolhidos a mao nao mede quase nada. O que mede e gerar
/// milhares de programas sem nenhum cuidado, perguntar quais tipam e rodar
/// todos.
/// </summary>
public class SolidezTestes
{
    /// <summary>
    /// O numero que importa e ZERO, em dez mil programas sorteados.
    /// </summary>
    [Fact]
    public void NenhumProgramaBemTipadoTrava()
    {
        var c = Gerador.Medir(7, 10_000);

        Assert.Equal(0, c.TiparamETravaram);
        Assert.True(c.Tiparam > 1_000, $"so {c.Tiparam} programas tiparam, a amostra ficou fraca");
    }

    /// <summary>
    /// E e zero com qualquer semente: o resultado nao depende de ter dado sorte
    /// com uma amostra.
    /// </summary>
    [Fact]
    public void EhZeroComQualquerSemente()
    {
        foreach (var semente in new ulong[] { 1, 42, 999, 12345 })
        {
            var c = Gerador.Medir(semente, 3_000);

            Assert.Equal(0, c.TiparamETravaram);
            Assert.True(c.Tiparam > 100);
        }
    }

    /// <summary>
    /// E com programas mais fundos tambem, que e onde as combinacoes ficam mais
    /// estranhas.
    /// </summary>
    [Fact]
    public void EhZeroComProgramasMaisFundos()
    {
        foreach (var profundidade in new[] { 2, 3, 5, 6 })
            Assert.Equal(0, Gerador.Medir(7, 2_000, profundidade).TiparamETravaram);
    }

    /// <summary>
    /// O PRECO: uma em cada cinco recusas e de um programa que rodava bem.
    ///
    /// Essa e a outra metade da historia. O sistema de tipos nao separa programa
    /// bom de programa ruim: ele separa o que ele CONSEGUE PROVAR que nao trava
    /// de todo o resto, e o resto tem muita coisa boa.
    /// </summary>
    [Fact]
    public void UmaEmCadaCincoRecusasEhDeProgramaQueRodavaBem()
    {
        var c = Gerador.Medir(7, 10_000);

        Assert.InRange(c.PrecoDaRecusa, 0.15, 0.30);
        Assert.True(c.NaoTiparamERodaram > 500);
    }

    /// <summary>
    /// O ACHADO que me corrigiu: o let-polimorfismo nao compra quase NADA em
    /// programa sorteado.
    ///
    /// Sao cinco programas a mais em cinquenta mil. Usar o mesmo nome em dois
    /// tipos diferentes quase nunca acontece por acaso, e e a coisa que todo
    /// programa de verdade faz o tempo todo. Medir numa amostra aleatoria e
    /// medir o quanto a amostra parece com o mundo.
    /// </summary>
    [Fact]
    public void OLetPolimorfismoQuaseNaoMudaOsNumerosDaAmostra()
    {
        var com = Gerador.Medir(7, 10_000);
        var sem = Gerador.Medir(7, 10_000, comPolimorfismo: false);

        Assert.True(com.Tiparam >= sem.Tiparam);
        Assert.True(com.Tiparam - sem.Tiparam < com.Tiparam / 100,
                    $"a diferenca foi de {com.Tiparam - sem.Tiparam} programas");
    }

    /// <summary>
    /// E o sistema sem polimorfismo tambem e solido: ele aceita menos e nenhum
    /// dos que aceita trava.
    /// </summary>
    [Fact]
    public void OSistemaMonomorficoTambemEhSolido()
    {
        Assert.Equal(0, Gerador.Medir(7, 5_000, comPolimorfismo: false).TiparamETravaram);
    }

    /// <summary>
    /// O gerador e deterministico: a mesma semente da os mesmos programas. Sem
    /// isso, a integracao continua nos tres sistemas estaria medindo coisas
    /// diferentes em cada um.
    /// </summary>
    [Fact]
    public void OGeradorEhDeterministico()
    {
        var primeira = new Gerador(7);
        var segunda = new Gerador(7);

        for (var i = 0; i < 200; i++)
            Assert.Equal(primeira.Qualquer(4).ToString(), segunda.Qualquer(4).ToString());

        Assert.Equal(Gerador.Medir(7, 1_000), Gerador.Medir(7, 1_000));
    }

    [Fact]
    public void SementesDiferentesDaoProgramasDiferentes()
    {
        var um = new Gerador(1).Qualquer(5).ToString();
        var outro = new Gerador(2).Qualquer(5).ToString();

        Assert.NotEqual(um, outro);
    }

    /// <summary>
    /// A amostra tem variedade: ela nao e feita so de numeros soltos. Sem isso,
    /// a medida de solidez seria sobre nada.
    /// </summary>
    [Fact]
    public void AAmostraTemVariedade()
    {
        var gerador = new Gerador(7);
        var tamanhos = new HashSet<int>();
        var formas = new HashSet<string>();

        for (var i = 0; i < 500; i++)
        {
            var termo = gerador.Qualquer(4);
            tamanhos.Add(termo.Tamanho());
            formas.Add(termo.GetType().Name);
        }

        Assert.True(tamanhos.Count > 10, $"so {tamanhos.Count} tamanhos diferentes");
        Assert.True(formas.Count >= 6, $"so {formas.Count} formas diferentes");
    }

    [Fact]
    public void OSorteioFicaDentroDoLimite()
    {
        var gerador = new Gerador(7);

        for (var i = 0; i < 1000; i++) Assert.InRange(gerador.Ate(10), 0, 9);
    }

    [Fact]
    public void SementeZeroNaoTravaOGerador()
    {
        var gerador = new Gerador(0);
        Assert.NotEqual(0u, gerador.Proximo());
    }
}
