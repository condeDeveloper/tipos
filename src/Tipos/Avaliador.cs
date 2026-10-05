namespace Conde.Tipos;

/// <summary>Um VALOR: o que um programa devolve quando termina.</summary>
public abstract record Valor;

public sealed record Inteiro(int Valor) : Valor
{
    public override string ToString() => Valor.ToString();
}

public sealed record Logico(bool Valor) : Valor
{
    public override string ToString() => Valor ? "verdadeiro" : "falso";
}

public sealed record Fechamento(string Parametro, Termo Corpo, Memoria Memoria) : Valor
{
    public override string ToString() => $"<fun {Parametro}>";
}

public sealed record Dois(Valor Esquerda, Valor Direita) : Valor
{
    public override string ToString() => $"({Esquerda}, {Direita})";
}

/// <summary>O ambiente de execucao: o valor de cada nome.</summary>
public sealed class Memoria
{
    private readonly Dictionary<string, Valor> mapa;

    public Memoria() => mapa = [];

    private Memoria(Dictionary<string, Valor> mapa) => this.mapa = mapa;

    public Memoria Com(string nome, Valor valor) => new(new Dictionary<string, Valor>(mapa) { [nome] = valor });

    public Valor? Buscar(string nome) => mapa.TryGetValue(nome, out var achado) ? achado : null;
}

/// <summary>
/// O AVALIADOR, que e o JUIZ deste repositorio.
///
/// O teorema que o sistema de tipos promete tem nome e e verificavel: programa
/// bem tipado NAO TRAVA. "Travar" aqui nao e metafora, e uma lista fechada de
/// situacoes concretas: aplicar algo que nao e funcao, somar algo que nao e
/// numero, desviar por algo que nao e logico, projetar algo que nao e par, usar
/// um nome que nao existe.
///
/// Entao a medida e direta: gerar milhares de programas, perguntar ao algoritmo
/// de inferencia quais deles tipam, rodar TODOS, e conferir que nenhum dos
/// tipados travou. O numero que importa e zero, e ele e contado e nao suposto.
///
/// E tem o outro lado: entre os que NAO tipam, muitos tambem nao travariam. O
/// sistema de tipos recusa programas corretos, e a medida diz quantos.
/// </summary>
public static class Avaliador
{
    public sealed record Resultado(Valor? Valor, string? Travou, bool SemFim, int Passos)
    {
        public bool Terminou => Valor is not null;
    }

    /// <param name="fundo">
    /// A PROFUNDIDADE maxima da recursao, que e um limite diferente do de
    /// passos e precisa existir junto com ele.
    ///
    /// Isso me custou uma integracao continua vermelha. Contar passos limita o
    /// TRABALHO e nao limita a PILHA: aplicacao aninhada cresce a pilha a cada
    /// nivel, e o omega aninha para sempre. Com limite de cinco mil passos, a
    /// mesma conta passava no Linux e no Windows e estourava a pilha no macOS,
    /// que tem pilha menor, derrubando o processo de teste inteiro em vez de
    /// falhar um teste.
    ///
    /// Duzentos niveis sao muito mais do que qualquer programa daqui precisa, e
    /// muito menos do que a menor das tres pilhas aguenta.
    /// </param>
    public static Resultado Avaliar(Termo termo, int limite = 200_000, int fundo = 200)
    {
        var passos = 0;
        var valor = Rodar(termo, new Memoria(), ref passos, limite, fundo, out var travou, out var semFim);

        return new Resultado(valor, travou, semFim, passos);
    }

    private static Valor? Rodar(Termo termo, Memoria memoria, ref int passos, int limite, int fundo,
                                out string? travou, out bool semFim)
    {
        travou = null;
        semFim = false;

        if (++passos > limite || fundo <= 0) { semFim = true; return null; }

        switch (termo)
        {
            case Numero n:
                return new Inteiro(n.Valor);

            case Booleano b:
                return new Logico(b.Valor);

            case Nome nome:
            {
                var valor = memoria.Buscar(nome.Id);
                if (valor is null) { travou = $"nome livre: {nome.Id}"; return null; }
                return valor;
            }

            case Funcao f:
                return new Fechamento(f.Parametro, f.Corpo, memoria);

            case Aplicacao a:
            {
                var alvo = Rodar(a.Alvo, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (alvo is null) return null;

                var argumento = Rodar(a.Argumento, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (argumento is null) return null;

                // A primeira forma de travar: aplicar o que nao e funcao.
                if (alvo is not Fechamento fechamento) { travou = $"aplicou {alvo}, que nao e funcao"; return null; }

                return Rodar(fechamento.Corpo, fechamento.Memoria.Com(fechamento.Parametro, argumento),
                             ref passos, limite, fundo - 1, out travou, out semFim);
            }

            case Deixe d:
            {
                var valor = Rodar(d.Valor, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (valor is null) return null;

                return Rodar(d.Corpo, memoria.Com(d.Id, valor), ref passos, limite, fundo - 1, out travou, out semFim);
            }

            case Se s:
            {
                var condicao = Rodar(s.Condicao, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (condicao is null) return null;

                if (condicao is not Logico logico) { travou = $"desviou por {condicao}, que nao e logico"; return null; }

                return Rodar(logico.Valor ? s.Entao : s.Senao, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
            }

            case Dupla p:
            {
                var esquerda = Rodar(p.Esquerda, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (esquerda is null) return null;

                var direita = Rodar(p.Direita, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (direita is null) return null;

                return new Dois(esquerda, direita);
            }

            case Projecao j:
            {
                var alvo = Rodar(j.Alvo, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (alvo is null) return null;

                if (alvo is not Dois dois) { travou = $"projetou {alvo}, que nao e par"; return null; }

                return j.Primeiro ? dois.Esquerda : dois.Direita;
            }

            case Soma m:
            {
                var esquerda = Rodar(m.Esquerda, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (esquerda is null) return null;

                var direita = Rodar(m.Direita, memoria, ref passos, limite, fundo - 1, out travou, out semFim);
                if (direita is null) return null;

                if (esquerda is not Inteiro a) { travou = $"somou {esquerda}, que nao e numero"; return null; }
                if (direita is not Inteiro b) { travou = $"somou {direita}, que nao e numero"; return null; }

                return new Inteiro(a.Valor + b.Valor);
            }

            default:
                travou = $"termo desconhecido: {termo}";
                return null;
        }
    }
}
