using Conde.Tipos;

namespace Conde.Tipos.Medidor;

/// <summary>
/// As medidas. O juiz e sempre o AVALIADOR: o sistema de tipos promete que
/// programa bem tipado nao trava, e a unica maneira honesta de conferir isso e
/// rodar os programas.
///
/// Tudo e contado em programas, unificacoes e nos de tipo. Nenhuma medida usa
/// relogio.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "tipos": Tipos(); break;
            case "solidez": Solidez(); break;
            case "recusa": Recusa(); break;
            case "polimorfismo": Polimorfismo(); break;
            case "ocorrencia": Ocorrencia(); break;
            case "explosao": Explosao(); break;
            case "tudo": Tipos(); Solidez(); Recusa(); Polimorfismo(); Ocorrencia(); Explosao(); break;
            default:
                Console.Error.WriteLine("medidas: tipos, solidez, recusa, polimorfismo, ocorrencia, explosao, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>O tipo principal de cada exemplo, inferido sem anotacao nenhuma.</summary>
    private static void Tipos()
    {
        Console.WriteLine("== o tipo principal, inferido sem nenhuma anotacao ==");
        Console.WriteLine();
        Console.WriteLine($"{"programa",-28}{"tipo",-30}{"unificacoes",12}");

        foreach (var (nome, termo) in Exemplos.QueTipam())
        {
            var r = Inferencia.Inferir(termo);
            Console.WriteLine($"{nome,-28}{r.Tipo?.ToString(),-30}{r.Unificacoes,12}");
        }

        Console.WriteLine();
        Console.WriteLine("nenhum desses programas tem uma anotacao de tipo. O algoritmo descobre o");
        Console.WriteLine("tipo PRINCIPAL, que e o mais geral de todos os que aquele termo tem, e e por");
        Console.WriteLine("isso que anotacao nunca e obrigatoria");
        Console.WriteLine();

        Console.WriteLine($"  e os que sao recusados, com o motivo:");
        Console.WriteLine($"  {"programa",-30}{"motivo",-46}");

        foreach (var (nome, termo) in Exemplos.QueNaoTipam())
            Console.WriteLine($"  {nome,-30}{Inferencia.Inferir(termo).Erro,-46}");

        Console.WriteLine();
    }

    /// <summary>A solidez, medida em milhares de programas aleatorios.</summary>
    private static void Solidez()
    {
        Console.WriteLine("== programa bem tipado nao trava, contado e nao suposto ==");
        Console.WriteLine();
        Console.WriteLine($"{"programas",12}{"tiparam",10}{"e travaram",12}{"e nao terminaram",18}");

        foreach (var quantos in new[] { 1_000, 10_000, 50_000 })
        {
            var c = Gerador.Medir(7, quantos);
            Console.WriteLine($"{c.Total,12:N0}{c.Tiparam,10:N0}{c.TiparamETravaram,12}{c.TiparamESemFim,18}");
        }

        Console.WriteLine();
        Console.WriteLine("a coluna do meio e ZERO em cinquenta mil programas sorteados sem nenhum");
        Console.WriteLine("cuidado. O teorema diz isso, e aqui ele e contado: nenhum programa aceito");
        Console.WriteLine("aplicou o que nao e funcao, somou o que nao e numero nem desviou por algo");
        Console.WriteLine("que nao e logico");
        Console.WriteLine();
        Console.WriteLine("a ultima coluna tambem e zero, e isso NAO e promessa do sistema: um");
        Console.WriteLine("programa bem tipado pode nao terminar. Aqui nao aparece porque a linguagem");
        Console.WriteLine("nao tem recursao, e sem recursao nenhum termo tipado roda para sempre");
        Console.WriteLine();

        foreach (var semente in new ulong[] { 1, 42, 12345 })
        {
            var c = Gerador.Medir(semente, 10_000);
            Console.WriteLine($"  semente {semente,-8} {c.Tiparam,6:N0} tiparam, {c.TiparamETravaram} travaram");
        }

        Console.WriteLine();
    }

    /// <summary>O preco: quantos programas bons o sistema recusa.</summary>
    private static void Recusa()
    {
        Console.WriteLine("== o preco: quantos programas BONS o sistema recusa ==");
        Console.WriteLine();
        Console.WriteLine($"{"programas",12}{"recusados",12}{"e rodavam bem",16}{"fracao",10}");

        foreach (var quantos in new[] { 1_000, 10_000, 50_000 })
        {
            var c = Gerador.Medir(7, quantos);
            Console.WriteLine($"{c.Total,12:N0}{c.NaoTiparam,12:N0}{c.NaoTiparamERodaram,16:N0}{c.PrecoDaRecusa,10:P1}");
        }

        Console.WriteLine();
        Console.WriteLine("essa e a outra metade da historia, e ela quase nunca aparece. O sistema de");
        Console.WriteLine("tipos nao separa programa bom de programa ruim: ele separa programa que ele");
        Console.WriteLine("CONSEGUE PROVAR que nao trava de todo o resto, e o resto tem muita coisa boa");
        Console.WriteLine();
        Console.WriteLine("o exemplo mais curto disso cabe numa linha:");
        Console.WriteLine();

        foreach (var (nome, termo) in new[]
                 {
                     ("ramo ruim que nunca roda", Exemplos.RamoRuimQueNuncaRoda()),
                     ("id nos dois tipos sem let", Exemplos.IdentidadeNosDoisTiposSemLet()),
                 })
        {
            var r = Avaliador.Avaliar(termo);
            Console.WriteLine($"  {nome}");
            Console.WriteLine($"    {termo}");
            Console.WriteLine($"    tipa?  nao: {Inferencia.Inferir(termo).Erro}");
            Console.WriteLine($"    roda?  sim: devolve {r.Valor}");
            Console.WriteLine();
        }
    }

    /// <summary>O que o let-polimorfismo compra, em programas.</summary>
    private static void Polimorfismo()
    {
        Console.WriteLine("== o que o let-polimorfismo compra ==");
        Console.WriteLine();
        Console.WriteLine($"{"programas",12}{"com let polim.",16}{"sem",8}{"a mais",10}");

        foreach (var quantos in new[] { 10_000, 50_000 })
        {
            var com = Gerador.Medir(7, quantos);
            var sem = Gerador.Medir(7, quantos, comPolimorfismo: false);

            Console.WriteLine($"{quantos,12:N0}{com.Tiparam,16:N0}{sem.Tiparam,8:N0}{com.Tiparam - sem.Tiparam,10:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("e isso me corrigiu. Eu esperava uma diferenca grande e ela e de CINCO");
        Console.WriteLine("programas em cinquenta mil: o let-polimorfismo nao compra quase nada em");
        Console.WriteLine("programa sorteado");
        Console.WriteLine();
        Console.WriteLine("o motivo e que usar o mesmo nome em DOIS tipos diferentes e uma coisa que");
        Console.WriteLine("quase nunca acontece por acaso. E e a coisa que todo programa de verdade");
        Console.WriteLine("faz o tempo todo, porque toda funcao util e usada em mais de um tipo");
        Console.WriteLine();
        Console.WriteLine("medir numa amostra aleatoria e medir o quanto a amostra parece com o");
        Console.WriteLine("mundo, e aqui ela nao parece");
        Console.WriteLine();

        Console.WriteLine("e a diferenca em programa escrito a mao:");
        Console.WriteLine();

        var termo = Exemplos.IdentidadeNosDoisTipos();
        Console.WriteLine($"  {termo}");
        Console.WriteLine($"    com polimorfismo:  {Inferencia.Inferir(termo)}");
        Console.WriteLine($"    sem polimorfismo:  {Inferencia.Inferir(termo, comPolimorfismo: false)}");
        Console.WriteLine();
        Console.WriteLine("a UNICA diferenca entre os dois sistemas e uma linha: se o tipo do valor e");
        Console.WriteLine("generalizado antes de entrar no ambiente. Com ela, cada uso do nome ganha");
        Console.WriteLine("variaveis frescas; sem ela, o primeiro uso decide o tipo para todos");
        Console.WriteLine();
        Console.WriteLine("e o mesmo programa escrito com aplicacao em vez de let nao tipa nos DOIS:");
        Console.WriteLine($"  {Exemplos.IdentidadeNosDoisTiposSemLet()}");
        Console.WriteLine($"    {Inferencia.Inferir(Exemplos.IdentidadeNosDoisTiposSemLet())}");
        Console.WriteLine();
        Console.WriteLine("so o let generaliza, e as duas formas rodam exatamente igual");
        Console.WriteLine();
    }

    /// <summary>O teste de ocorrencia, ligado e desligado.</summary>
    private static void Ocorrencia()
    {
        Console.WriteLine("== o teste de ocorrencia e o tipo infinito ==");
        Console.WriteLine();

        var autoAplicacao = Exemplos.AutoAplicacao();

        Console.WriteLine($"  {autoAplicacao}");
        Console.WriteLine($"    com teste:  {Inferencia.Inferir(autoAplicacao)}");
        Console.WriteLine($"    sem teste:  {Inferencia.Inferir(autoAplicacao, comTesteDeOcorrencia: false)}");
        Console.WriteLine();
        Console.WriteLine("sem o teste, o algoritmo ACEITA a auto-aplicacao e devolve um tipo. O tipo");
        Console.WriteLine("esta errado: ele e a primeira volta de uma coisa que nao termina");
        Console.WriteLine();

        Tipo a = new Variavel(0), b = new Variavel(1);
        var infinito = new Seta(a, b);
        var resultado = Unifica.Unificar(a, infinito, comTesteDeOcorrencia: false);

        Console.WriteLine($"  unificar {a} com {infinito}, sem o teste: {resultado.Substituicao}");
        Console.WriteLine();
        Console.WriteLine($"  {"aplicacoes",12}{"tamanho do tipo",18}");

        var atual = a;
        for (var i = 0; i <= 10; i++)
        {
            Console.WriteLine($"  {i,12}{atual.Tamanho(),18:N0}");
            atual = resultado.Substituicao!.Aplicar(atual);
        }

        Console.WriteLine();
        Console.WriteLine("o tipo ganha dois nos a cada aplicacao e NUNCA estabiliza. Num tipo bem");
        Console.WriteLine("formado, aplicar a substituicao duas vezes da o mesmo que aplicar uma: e");
        Console.WriteLine("esse ponto fixo que nao existe aqui");
        Console.WriteLine();
        Console.WriteLine("cresce de dois em dois, e nao dobrando, porque a aplicacao desta");
        Console.WriteLine("substituicao e de UM PASSO. A versao recursiva parece mais completa e");
        Console.WriteLine("estouraria a pilha em vez de mostrar o crescimento");
        Console.WriteLine();
        Console.WriteLine("com o teste ligado, a unificacao recusa de cara, e a recusa e a resposta");
        Console.WriteLine("certa: nao existe tipo finito para essa equacao");
        Console.WriteLine();

        var comTeste = Unifica.Unificar(a, infinito);
        Console.WriteLine($"  com o teste: {comTeste.Erro}");
        Console.WriteLine();
    }

    /// <summary>O tamanho do tipo, que cresce exponencialmente.</summary>
    private static void Explosao()
    {
        Console.WriteLine("== onde a inferencia fica cara: no TAMANHO DA RESPOSTA ==");
        Console.WriteLine();
        Console.WriteLine($"{"andares",10}{"nos do termo",14}{"nos do tipo",14}{"unificacoes",13}{"passos",10}");

        for (var andares = 0; andares <= 14; andares++)
        {
            var termo = Exemplos.TorreDePares(andares);
            var r = Inferencia.Inferir(termo);

            Console.WriteLine($"{andares,10}{termo.Tamanho(),14:N0}{r.Tipo!.Tamanho(),14:N0}" +
                              $"{r.Unificacoes,13:N0}{r.PassosDeUnificacao,10:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("o termo cresce LINEAR e o tipo cresce DOBRANDO. Com quatorze andares o");
        Console.WriteLine("programa tem algumas dezenas de nos e o tipo dele tem dezenas de milhares");
        Console.WriteLine();
        Console.WriteLine("e olhe a coluna das unificacoes: ZERO em todas as linhas. O algoritmo nao");
        Console.WriteLine("esta trabalhando duro, ele so esta montando um tipo gigante. A inferencia");
        Console.WriteLine("de Hindley e Milner tem complexidade exponencial no pior caso, e o custo");
        Console.WriteLine("nao esta na dificuldade do problema: esta no TAMANHO DA RESPOSTA, que nao");
        Console.WriteLine("cabe menor");
        Console.WriteLine();
        Console.WriteLine("na pratica isso nunca acontece, porque programa de verdade nao tem torre de");
        Console.WriteLine("pares. O pior caso existe, e ele mora num canto que ninguem visita");
        Console.WriteLine();
    }
}
