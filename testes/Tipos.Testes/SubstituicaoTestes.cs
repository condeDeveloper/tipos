using Conde.Tipos;
using Xunit;

namespace Conde.Tipos.Testes;

public class SubstituicaoTestes
{
    private static readonly Tipo A = new Variavel(0);
    private static readonly Tipo B = new Variavel(1);
    private static readonly Tipo C = new Variavel(2);

    [Fact]
    public void AplicarTrocaOQueSeSabe()
    {
        var s = Substituicao.De(0, Tipo.Inteiro);

        Assert.Equal(Tipo.Inteiro, s.Aplicar(A));
        Assert.Equal(B, s.Aplicar(B));
        Assert.Equal(new Seta(Tipo.Inteiro, B), s.Aplicar(new Seta(A, B)));
        Assert.Equal(new Par(Tipo.Inteiro, Tipo.Inteiro), s.Aplicar(new Par(A, A)));
    }

    /// <summary>
    /// Compor nao e juntar os mapas. O que a primeira descobriu precisa ser
    /// atualizado com o que a segunda descobriu depois, ou o tipo final sai com
    /// um buraco.
    /// </summary>
    [Fact]
    public void ComporAtualizaOLadoDireito()
    {
        var primeira = Substituicao.De(0, B);
        var segunda = Substituicao.De(1, Tipo.Inteiro);

        var junto = primeira.Depois(segunda);

        Assert.Equal(Tipo.Inteiro, junto.Aplicar(A));
        Assert.Equal(Tipo.Inteiro, junto.Aplicar(B));

        // juntar os mapas sem atualizar deixaria "a" apontando para "b"
        Assert.NotEqual(B, junto.Aplicar(A));
    }

    [Fact]
    public void AVaziaNaoMudaNada()
    {
        var tipo = new Seta(A, new Par(B, Tipo.Logico));
        Assert.Equal(tipo, Substituicao.Vazia.Aplicar(tipo));
        Assert.Equal(0, Substituicao.Vazia.Quantidade);
    }

    /// <summary>
    /// Aplicar a um esquema NAO pode mexer nas variaveis quantificadas: elas sao
    /// ligadas ali. Trocar uma delas por causa de uma descoberta de fora e o bug
    /// de captura de variavel, e ele faria a identidade deixar de servir para
    /// qualquer tipo.
    /// </summary>
    [Fact]
    public void AplicarNaoMexeNoQueEstaQuantificado()
    {
        var esquema = new Esquema(new[] { 0 }, new Seta(A, B));
        var s = Substituicao.De(0, Tipo.Inteiro).Depois(Substituicao.De(1, Tipo.Logico));

        var depois = s.Aplicar(esquema);

        // o "a" ligado continua ligado; o "b" livre virou bool
        Assert.Equal(new Seta(A, Tipo.Logico), depois.Corpo);
        Assert.Equal(new[] { 0 }, depois.Quantificados);
    }

    [Fact]
    public void OAmbienteGuardaEsquemas()
    {
        var ambiente = new Ambiente().Com("id", new Esquema(new[] { 0 }, new Seta(A, A)));

        Assert.NotNull(ambiente.Buscar("id"));
        Assert.Null(ambiente.Buscar("outro"));
        Assert.Equal("para todo a. a -> a", ambiente.Buscar("id")!.ToString());
    }

    [Fact]
    public void OAmbienteEhImutavel()
    {
        var vazio = new Ambiente();
        var com = vazio.Com("x", Esquema.Simples(Tipo.Inteiro));

        Assert.Null(vazio.Buscar("x"));
        Assert.NotNull(com.Buscar("x"));
    }

    /// <summary>
    /// As variaveis LIVRES do ambiente sao as que a generalizacao nao pode
    /// quantificar: elas ainda podem ser decididas por outra parte do programa.
    /// </summary>
    [Fact]
    public void AsVariaveisLivresIgnoramAsQuantificadas()
    {
        var ambiente = new Ambiente()
            .Com("id", new Esquema(new[] { 0 }, new Seta(A, A)))
            .Com("preso", Esquema.Simples(new Seta(B, C)));

        var livres = ambiente.VariaveisLivres();

        Assert.DoesNotContain(0, livres);
        Assert.Contains(1, livres);
        Assert.Contains(2, livres);
    }

    [Fact]
    public void AEscritaDaSubstituicaoEhLegivel()
    {
        Assert.Equal("{}", Substituicao.Vazia.ToString());
        Assert.Equal("{a := int}", Substituicao.De(0, Tipo.Inteiro).ToString());
    }
}
