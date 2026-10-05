# tipos

Inferência de tipos do zero em C# e .NET 8: unificação, algoritmo W de
Hindley-Milner, let-polimorfismo e um avaliador. O juiz é o **avaliador**: o
sistema de tipos promete que programa bem tipado não trava, e a única maneira
honesta de conferir isso é rodar os programas.

```
$ dotnet medidor.dll solidez

   programas   tiparam  e travaram  e nao terminaram
       1,000       341           0                 0
      10,000     3,729           0                 0
      50,000    18,702           0                 0
```

Cinquenta mil programas sorteados sem nenhum cuidado. Dezoito mil deles tipam, e
**nenhum** aplicou o que não é função, somou o que não é número ou desviou por
algo que não é lógico. O teorema diz isso; aqui ele é contado.

## A outra metade da história

```
$ dotnet medidor.dll recusa

   programas   recusados   e rodavam bem    fracao
       1,000         659             125    19.0 %
      10,000       6,271           1,312    20.9 %
      50,000      31,298           6,466    20.7 %
```

**Uma em cada cinco** recusas é de um programa que rodava bem. O sistema de tipos
não separa programa bom de programa ruim: ele separa o que ele **consegue
provar** que não trava de todo o resto, e o resto tem muita coisa boa.

O exemplo mais curto cabe numa linha:

```
  se verdadeiro entao 1 senao (1 + verdadeiro)
    tipa?  nao: bool nao e int
    roda?  sim: devolve 1
```

O sistema olha os dois caminhos, e o programa só anda por um.

## O par de programas que roda igual e tipa diferente

```
  let id = fun x -> x in ((id 1), (id verdadeiro))        tipa:  (int, bool)
  (fun id -> ((id 1), (id verdadeiro)) fun x -> x)        nao tipa
```

As duas formas fazem exatamente a mesma conta e devolvem exatamente o mesmo
valor. Só o **let** generaliza, e é essa escolha, e não a execução, que decide o
destino das duas no sistema de tipos.

```csharp
var esquemaDoValor = ComPolimorfismo
    ? Generalizar(depois, s1.Aplicar(doValor))
    : Esquema.Simples(s1.Aplicar(doValor));
```

Essa é a **única** diferença entre o sistema polimórfico e o monomórfico. Com a
generalização, cada uso do nome ganha variáveis frescas; sem ela, o primeiro uso
decide o tipo para todos.

## O tipo principal, inferido sem nenhuma anotação

```
$ dotnet medidor.dll tipos

programa                    tipo                           unificacoes
identidade                  a -> a                                   0
constante                   a -> b -> a                              0
composicao                  (d -> e) -> (c -> d) -> c -> e           2
id nos dois tipos           (int, bool)                              2
projecao                    int                                      1
```

Nenhum desses programas tem uma anotação. O algoritmo descobre o tipo
**principal**, que é o mais geral de todos os que aquele termo tem, e é por isso
que anotação nunca é obrigatória.

## O teste de ocorrência e o tipo infinito

```
$ dotnet medidor.dll ocorrencia

  fun x -> (x x)
    com teste:  erro: ocorrencia: a aparece dentro de a -> b
    sem teste:  ((a -> b) -> b) -> b

  unificar a com a -> b, sem o teste: {a := a -> b}

    aplicacoes   tamanho do tipo
             0                 1
             1                 3
             2                 5
            10                21
```

Sem o teste, a unificação **aceita** e devolve uma substituição que nunca
estabiliza. Num tipo bem formado, aplicar duas vezes dá o mesmo que aplicar uma;
é esse ponto fixo que não existe aqui. A recusa é a resposta certa: não existe
tipo finito para a equação `a = a -> b`.

## Onde a inferência fica cara

```
$ dotnet medidor.dll explosao

   andares  nos do termo   nos do tipo  unificacoes    passos
         0             4             3            0         0
         4            20            63            0         0
         8            36         1,023            0         0
        12            52        16,383            0         0
        14            60        65,535            0         0
```

O termo cresce **linear** e o tipo cresce **dobrando**. Com quatorze andares, o
programa tem sessenta nós e o tipo dele tem sessenta e cinco mil.

E a coluna das unificações é **zero** em todas as linhas. O algoritmo não está
trabalhando duro: ele só está montando um tipo gigante. A inferência de
Hindley-Milner tem complexidade exponencial no pior caso, e o custo não está na
dificuldade do problema, está no **tamanho da resposta**, que não cabe menor.

## O que as medidas me corrigiram

**O let-polimorfismo quase não compra nada em programa sorteado.** Eu esperava
uma diferença grande entre o sistema com e sem generalização, e ela é de **cinco
programas em cinquenta mil**:

```
$ dotnet medidor.dll polimorfismo

   programas  com let polim.     sem    a mais
      10,000           3,729   3,728         1
      50,000          18,702  18,697         5
```

Usar o mesmo nome em dois tipos diferentes quase nunca acontece por acaso. E é a
coisa que todo programa de verdade faz o tempo todo, porque toda função útil é
usada em mais de um tipo. Medir numa amostra aleatória é medir o quanto a
amostra parece com o mundo, e aqui ela não parece.

**O tipo da torre de pares não é `2^n`, é `2^(n+2) - 1`.** Eu escrevi o teste com
a potência errada porque esqueci que o andar zero já começa com três nós: a
identidade é uma seta, não um átomo. O crescimento é o que eu esperava, o ponto
de partida não era.

**Aplicar a substituição recursivamente estoura a pilha em vez de mostrar o
problema.** A versão recursiva de "trocar a variável pelo que ela virou" parece
mais completa, e com o teste de ocorrência desligado a substituição pode mandar
`a` para um tipo que contém `a`. A troca de **um passo** é o que deixa o
crescimento aparecer como número, de dois em dois nós, em vez de aparecer como
queda do processo.

**O tipo da identidade na torre sai como `b -> b`, e não `a -> a`.** Usar um nome
polimórfico instancia o esquema com variáveis frescas, então o número da variável
não é parte da resposta. O teste que comparava a string estava testando o
contador, não o tipo; ele passou a comparar a estrutura.

## Por que o juiz é o avaliador

"Travar" não é metáfora aqui: é uma lista fechada de cinco situações concretas,
aplicar o que não é função, somar o que não é número, desviar por algo que não é
lógico, projetar o que não é par e usar um nome que não existe. Cada uma delas
tem um teste.

E a promessa do sistema é sobre **todos** os programas, então conferir oito
exemplos escolhidos à mão não mede quase nada. O que mede é gerar milhares de
programas sem nenhum cuidado, perguntar quais tipam e rodar todos.

O sorteio é de um gerador congruente escrito aqui, e não do sorteio da
biblioteca. Isso não é desconfiança: a documentação do sorteio de biblioteca não
promete a mesma sequência entre versões nem entre sistemas, e a integração
contínua roda em três. Com o gerador próprio, a semente 7 produz exatamente os
mesmos programas em todos.

Nenhuma medida aqui usa relógio. Todas são contagem de programas, de unificações
ou de nós de tipo.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Tipo.cs` | os tipos, as variáveis e o esquema que carrega o polimorfismo |
| `Termo.cs` | a linguagem inteira: dez formas e nada mais |
| `Substituicao.cs` | o que já se descobriu, a composição e o ambiente |
| `Unifica.cs` | a unificação e o teste de ocorrência, que dá para desligar |
| `Inferencia.cs` | o algoritmo W, com o let-polimorfismo numa linha |
| `Avaliador.cs` | o interpretador, que é o juiz |
| `Exemplos.cs` | os programas que valem a pena olhar um por um |
| `Gerador.cs` | o sorteio determinístico e a medida de solidez |

## Como rodar

```
dotnet test testes/Tipos.Testes/Tipos.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `tipos`, `solidez`, `recusa`, `polimorfismo`, `ocorrencia`,
`explosao` e `tudo`.

## Licença

MIT.
