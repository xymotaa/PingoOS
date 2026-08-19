# Registro de Alterações

## [2026-08-18] Painel: barra do gráfico sumia com o servidor em cultura pt-BR (versão 1.0.0.20)

### Problema
Depois da correção anterior (piso de altura diferenciado entre dia com/sem venda), o usuário
continuou vendo uma barra sumir — mas só em certos casos, dependendo de qual dia tinha o maior
valor da semana. A causa: `height: @altura%` interpola um `double` usando a cultura da thread do
servidor. Com o servidor em `pt-BR`, um valor como `55.555...` vira o texto `"55,555..."` — CSS não
aceita vírgula como separador decimal, então `style="height: 55,55%"` é uma declaração **inválida**
que o navegador descarta por inteiro, e a barra fica sem altura nenhuma (0px, visualmente "sumida").

Isso só acontecia quando a proporção calculada dava um número com casas decimais — daí parecer
"depender de qual dia é maior": quando o maior valor da semana e o valor do dia menor formavam uma
razão redonda (ex: exatamente 100% ou o piso fixo de 2%/8%), o texto não tinha vírgula e funcionava
por acaso; qualquer proporção fracionária (ex: 50/90 = 55,55...%) quebrava.

### Solução
`altura` agora é formatado explicitamente com `CultureInfo.InvariantCulture` antes de entrar no
`style`, garantindo ponto decimal sempre, independente da cultura configurada no servidor. Conferido
que o resto do projeto não tem o mesmo padrão de risco (só um outro lugar interpola número em CSS
inline, `Faturamento/Index.cshtml`, e já usava `InvariantCulture` corretamente).

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Views/Home/Index.cshtml` | `altura` formatado com `InvariantCulture` antes de entrar no `style="height: ...%"` |
| `VERSION.txt` | `1.0.0.20` |

### Resultado
Reproduzido o bug exato relatado pelo usuário (venda de R$90 hoje, pagamento de OS de R$50 numa
sexta anterior) em cópia do banco: antes da correção, o HTML gerado tinha `style="height:
55,55555555555556%"` (vírgula, CSS inválido); depois, `style="height: 55.55555555555556%"` (ponto,
válido). Build sem avisos.

**Nota**: por acordo com o usuário, esta entrega fica só no `main` — nenhuma tag/Release
criada/atualizada.

## [2026-08-18] Editar/excluir venda com rastro de auditoria; link removido da tela de senha (versão 1.0.0.19)

### Esqueci minha senha
Removido o link/ícone "Voltar para o login" do topo da tela — pedido do usuário.

### Editar e excluir vendas
Até agora, uma venda finalizada era definitiva — erro de digitação (produto errado, quantidade
errada, forma de pagamento errada) não tinha conserto sem editar o banco direto. Adicionado:

- **Editar venda**: reaproveita a mesma tela/JS do PDV (Caixa/Index + caixa.js) — o carrinho nasce
  preenchido com os itens da venda, forma de pagamento e valor recebido já marcados; pode adicionar,
  remover ou ajustar itens livremente, igual a uma venda nova. Salvar devolve ao estoque tudo que a
  venda original tinha baixado (como uma entrada de estorno, nunca editando a movimentação
  original) e dá baixa de novo com os itens da edição.
- **Excluir venda**: soft-delete (`Venda.Excluida`) — nunca um DELETE físico. Devolve o estoque da
  mesma forma que a edição. A venda some da listagem e de qualquer soma (Fechamento de Caixa,
  Painel, Faturamento), mas o registro continua existindo e consultável pelo histórico.
- **Histórico por venda**: tela nova (ícone ao lado de editar/excluir) mostrando cada evento
  (criada, editada, excluída) com data/hora e quem fez — nunca reescreve um evento antigo, só
  acrescenta. Mesmo espírito do histórico de movimentações do Estoque, mas essa é a primeira vez
  que o projeto tem uma trilha de auditoria de edição (não existia esse padrão antes).
- Linha da lista de Vendas ficou clicável (vai para editar), com os três ícones — editar, histórico,
  excluir — ao lado do total, seguindo o mesmo padrão visual das outras telas do sistema.

Sem trava de prazo: qualquer venda pode ser editada/excluída, independente da data — decisão do
usuário, por simplicidade.

### Bug encontrado durante a implementação
`FechamentoCaixaController` e outros dois pontos (`HomeController`: vendas de hoje, desempenho
semanal, atividades recentes; `FaturamentoController`: total do ano) somavam `Context.Vendas` sem
filtrar `Excluida` — uma venda soft-deleted continuaria contando nesses totais até esta correção.
Não chegou a ser um problema em produção (a funcionalidade de excluir venda é nova nesta mesma
entrega), mas seria um bug real assim que a primeira exclusão acontecesse.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Views/Conta/EsqueciSenha.cshtml` | remove o link "Voltar para o login" |
| `Models/Venda.cs` | `Excluida`, `ExcluidaPorId`, `DataExclusao`; novo `HistoricoVenda` + `TiposEventoVenda` |
| `Data/AppDbContext.cs` | `DbSet<HistoricoVenda>`, relações e `OnDelete` |
| `Migrations/20260818201625_AddEdicaoExclusaoVendas.cs` | migration nova, só adiciona (sem impacto em dados existentes) |
| `Data/EstoqueServico.cs` | `EstornarItensVenda` — devolve estoque via entrada nova, nunca edita movimentação antiga |
| `Controllers/CaixaController.cs` | `EditarVenda`, `SalvarEdicao`, `ExcluirVenda`, `HistoricoVenda`; `Vendas()`/`Finalizar` passam a registrar/filtrar `HistoricoVenda`/`Excluida` |
| `Controllers/HomeController.cs`, `Controllers/FaturamentoController.cs`, `Controllers/FechamentoCaixaController.cs` | somas de `Vendas` passam a excluir `Excluida` |
| `Views/Caixa/Vendas.cshtml`, `Views/Caixa/Index.cshtml`, `Views/Caixa/HistoricoVenda.cshtml` (novo) | linha clicável + ícones de ação; modo de edição na tela de PDV; tela de histórico |
| `wwwroot/js/caixa.js`, `wwwroot/js/vendas.js` (novo) | carrinho pré-carregado em modo edição; linha clicável |
| `VERSION.txt` | `1.0.0.19` |

### Resultado
Testado em cópia do banco de dev: editei uma venda (quantidade 1→3, pagamento dinheiro→PIX) e
confirmei no banco que a movimentação de estoque original permaneceu intacta, um estorno (entrada)
e uma nova saída foram lançados, e o histórico registrou o total antes/depois com o usuário. Excluí
outra venda e confirmei o estorno do estoque, o desaparecimento da listagem e do Fechamento de
Caixa daquele dia, e que o histórico continua acessível com o motivo informado. Confirmado 404 ao
tentar reabrir edição de venda já excluída. Migration aplicada no banco de dev real (com backup
prévio) sem alterar as 3 vendas reais existentes. Build sem avisos.

**Nota**: por acordo com o usuário, esta entrega fica só no `main` — nenhuma tag/Release
criada/atualizada, isso só acontece quando o usuário pedir.

## [2026-08-18] Painel: barra de venda pequena ficava indistinguível de dia sem venda (versão 1.0.0.18)

### Problema
No gráfico "Desempenho Semanal" do Painel, a altura de cada barra é proporcional ao maior valor da
semana (`Total / maiorValorSemana * 100`), com um piso mínimo de 2% para a barra nunca desaparecer
de vez. O piso valia tanto para dias sem nenhuma venda quanto para dias com venda real, mas pequena
frente ao maior dia — usuário reportou um dia com R$ 50,00 de venda com a barra do tamanho de um
dia zerado (a proporção real, `50 / valor_do_maior_dia * 100`, ficava abaixo de 2% sempre que o
maior dia passava de ~R$ 2.500, o que é comum). O valor em si estava certo (visível no tooltip/
rótulo ao passar o mouse); só a barra não refletia a diferença entre "teve venda" e "não teve".

### Solução
Separado o piso mínimo por caso: dia sem nenhuma venda continua com 2% de altura, numa cor neutra
(cinza); dia com qualquer valor positivo tem piso de 8%, na cor de destaque já usada — mesmo que a
proporção real calculada seja menor que isso. Diferença agora perceptível tanto na altura quanto na
cor, independente de quão discrepante o maior dia da semana seja.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Views/Home/Index.cshtml` | piso de altura e cor da barra dependem de `ponto.Total > 0`, não só do piso fixo de 2% |
| `VERSION.txt` | `1.0.0.18` |

### Resultado
Testado em cópia do banco de dev: venda de R$ 100,00 (soma de duas vendas de teste) numa sexta e
R$ 3.572,50 numa terça — sexta renderizou com 8% de altura e cor de destaque, dias sem venda com 2%
e cor neutra, terça com 100%. Diferença visualmente clara entre os três casos. Dados de teste
removidos ao final; banco de dev real não foi tocado. Build sem avisos.

**Nota**: por acordo com o usuário nesta sessão, mudanças de código passam a ficar só no `main` —
nenhuma tag/Release é criada ou atualizada automaticamente; isso só acontece quando o usuário pedir
explicitamente.

## [2026-08-18] PingoInstaller: arte ASCII removida (virava "????" no Windows 10) (versão 1.0.0.17)

### Problema
A versão anterior do `PingoInstaller.exe` (1.0.0.16) usava uma arte feita de caracteres em blocos
Unicode/braille, além de alguns símbolos gráficos (linha decorativa `════════`, seta `▶`) e texto
acentuado normal. Testado numa máquina Windows 10 real pela primeira vez: a arte e os símbolos
gráficos apareceram como `????` — a fonte padrão do console do Windows 10 (Consolas/Terminal
legado) não tem esses glifos. `Console.OutputEncoding = Encoding.UTF8` resolve a codificação, mas
não resolve a fonte não ter o desenho do caractere.

### Solução
Removida a arte por completo. Layout novo: "PingoOS" grande e centralizado no topo — agora
desenhado só com caracteres ASCII puro (letras feitas de `_`, `|`, `\`, `/`, sem nenhum bloco
Unicode), painel de máquina/IP/usuário/data-hora centralizado logo abaixo, e o menu (ou a pergunta
Instalar y/n) centralizado em seguida — tudo em uma coluna só, sem a composição lado a lado de
antes. A seta de seleção do menu virou `>` (ASCII) em vez de `▶`. Todo texto exibido na tela foi
revisado e reescrito sem acentuação (`Nao` em vez de `Não`, `Servico` em vez de `Serviço`, etc) —
decisão para zerar qualquer risco residual, já que a máquina de teste mostrou `????` na tela
inteira, não só na arte.

### Outro problema encontrado durante o teste
`InfoMaquina.ObterIpLocal()` (conexão UDP só pra descobrir a interface de rede local, sem tráfego
real) podia demorar vários segundos para retornar dependendo da configuração de rede da máquina,
atrasando visivelmente a primeira tela. Corrigido: a chamada agora roda com um timeout de 500ms —
se não responder a tempo, mostra "indisponivel" em vez de travar a tela de boas-vindas.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `PingoInstaller/AsciiArt.txt` | removido |
| `PingoInstaller/Logo.cs` (novo) | "PingoOS" em ASCII puro, substitui a arte |
| `PingoInstaller/Tela.cs` | remove leitura de recurso embutido; adiciona `EscreverCentralizado` |
| `PingoInstaller/Program.cs` | layout em coluna única, centralizado (logo, info, menu) |
| `PingoInstaller/Menu.cs` | seta `▶` → `>`; "Não" → "Nao" |
| `PingoInstaller/AcoesServico.cs`, `Instalador.cs` | textos de tela sem acentuação |
| `PingoInstaller/InfoMaquina.cs` | timeout de 500ms na detecção de IP local |
| `PingoInstaller/PingoInstaller.csproj` | remove referência ao `AsciiArt.txt` como recurso embutido |
| `VERSION.txt` | `1.0.0.17` |

### Resultado
Testado neste ambiente via Wine + pty simulado: tela de "Instalar PingoOS?" e o menu de 5 opções
(instalação já existente) renderizam corretamente, logo alinhada como bloco único, sem nenhum
caractere fora do ASCII básico. Corrigida uma armadilha do próprio processo de teste (ruído do
stderr do Wine intercalado no mesmo stream mascarava linhas do app no parser usado para inspecionar
visualmente — não era um bug do app). **Reportado pelo usuário rodando numa máquina Windows 10
real** que a versão anterior mostrava "????" — esta versão ainda não foi confirmada na mesma
máquina real após a correção. Build sem avisos.

## [2026-08-18] Novo instalador Windows com interface de terminal (versão 1.0.0.16)

### Motivação
O `install.bat` funciona, mas é um `.bat` puro: verde monocromático, sem arte, sem menu — só
`echo`s sequenciais. Pedido explícito: uma tela de boas-vindas com arte ASCII, painel de
informações da máquina (IP, usuário, data/hora) e um menu de verdade (Instalar y/n na primeira
vez; Atualizar/Reiniciar/Resetar senha/Desligar depois de instalado), navegável por seta ou tecla
numérica.

### Solução
Novo projeto `PingoInstaller/` — console app .NET, publicado como single-file self-contained
(`win-x64`), com manifesto pedindo elevação (UAC) automática. Faz exatamente os mesmos passos do
`install.bat` (verificar/instalar .NET e Git, clonar a última tag publicada, publicar preservando
o `loja.db`, registrar como Serviço do Windows), só que orquestrados em C# com uma TUI de verdade:

- Arte ASCII (embutida no binário, não como arquivo solto) à esquerda; "PingoOS" grande e o painel
  de máquina/IP/usuário/data-hora à direita.
- Detecta se já está instalado (`codigo\.git` válido + `ListasCompras.exe` presente): se não,
  pergunta "Instalar PingoOS?" (Sim/Não, navegável por seta ou tecla Y/N/Enter); se já está,
  pula direto para o menu de 5 opções (Atualizar/reinstalar, Reiniciar servidor, Resetar senha
  admin, Desligar servidor, Sair), navegável por seta ↑↓ ou pela tecla numérica da opção.
- "Resetar senha admin" chama a rotina que já existe no sistema (`ListasCompras.exe
  redefinir-senha`), sem duplicar lógica.
- Janela estreita demais para a arte + painel lado a lado mostra um aviso pedindo para maximizar,
  em vez de desenhar sobreposto.

### Decisões e por quê
- **.exe em vez de .bat melhorado**: cores, navegação por seta e composição de arte + texto lado a
  lado não são confiáveis em `.bat` puro entre versões diferentes do Windows — decisão do usuário
  ao comparar as duas opções.
- **Distribuído como GitHub Release** (binário anexado a uma tag), não como texto direto do repo:
  um `.exe` precisa existir compilado antes de ser baixado, diferente do `.bat`, que é lido direto
  do repositório. Isso também significa que criar/atualizar a Release passa a ser manual (ou via
  CI, se algum dia for automatizado) — diferente do `.bat`, que sempre reflete o `main` na hora.
- **install.bat continua existindo em paralelo** — decisão do usuário, para não depender só do
  `.exe` novo até ele ser validado numa loja de verdade.
- **Arte ASCII fornecida pelo próprio usuário** (caracteres em blocos Unicode/braille) — não usei a
  arte do anexo original de personagem de anime por ser recorte de imagem de terceiro com direitos
  autorais; o usuário forneceu uma alternativa própria em blocos Unicode.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `PingoInstaller/` (novo projeto) | console app .NET: `Program.cs`, `Tela.cs`, `Menu.cs`, `Instalador.cs`, `AcoesServico.cs`, `Processos.cs`, `Config.cs`, `InfoMaquina.cs`, `AsciiArt.txt`, `app.manifest` |
| `README.md` | instrução de instalação Windows atualizada para citar o `.exe` como opção recomendada |
| `ROADMAP.md` | nota na seção "Atualização automática" sobre a nova via de instalação |
| `VERSION.txt` | `1.0.0.16` |

### Resultado
Testado neste ambiente via Wine + pty simulado (não há máquina Windows real disponível aqui):
renderização da arte + painel confirmada visualmente (script de captura ANSI → grade de texto),
navegação por seta e por tecla numérica testadas isoladamente no menu de 5 opções e na pergunta
Sim/Não, ambas funcionando corretamente. Corrigido um bug real encontrado nessa revisão: `setx
PATH "%PATH%;..."` não expande `%PATH%` quando chamado via `Process.Start` (só funciona dentro de
um `.bat` interpretado pelo `cmd.exe`) — corrigido para ler o PATH de máquina real antes de
escrever. Build sem avisos. **Ainda não testado numa máquina Windows real** — pendência anotada
para quando o `.exe` for publicado e alguém rodar de verdade.

## [2026-08-18] Setas de campo numérico removidas em todo o sistema (versão 1.0.0.15)

### Problema
Vários `<input type="number">` (Estoque, Pedidos, Documento/Ver) já escondiam as setas de
incremento/decremento via `<style>` local em cada view — mas nem toda tela tinha essa regra.
`Documento/Add.cshtml` (Orçamento/OS, campo "Validade do orçamento") não tinha, então mostrava as
setas nativas do navegador.

### Solução
Regra movida para `site.css`, carregado por todas as telas (direto ou via partial
`_HeadTailwind`), cobrindo Webkit (Chrome/Edge/Safari) e Firefox de uma vez: nenhum campo numérico
do sistema mostra mais as setas, em nenhuma tela — Caixa, Orçamento/OS, Estoque, Pedidos e
qualquer outra que use `type="number"` no futuro, sem precisar repetir a regra por view.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/css/site.css` | regra global `input[type="number"]` sem setas (Webkit + Firefox) |
| `VERSION.txt` | `1.0.0.15` |

### Resultado
Publicado e confirmado via curl que o CSS é servido com a nova regra. Mudança puramente visual,
sem impacto em cálculo ou submit. Build sem avisos.

## [2026-08-18] Caixa: campo de desconto sem nenhum efeito/borda ao focar (versão 1.0.0.14)

### Ajuste
A correção anterior trocou o outline azul do navegador pelo anel verde institucional (padrão do
resto do sistema). O usuário preferiu não ter efeito nenhum: o campo agora fica visualmente igual
focado ou não, sem anel, sem borda, sem sombra.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/js/caixa.js` | classe do input de desconto sem `focus:ring`/`focus:shadow` |
| `VERSION.txt` | `1.0.0.14` |

### Resultado
Mudança puramente visual (CSS). Build sem avisos.

## [2026-08-18] Caixa: anel de foco azul do navegador no campo de desconto (versão 1.0.0.13)

### Problema
O campo "Desc. %" de cada item, ao ser clicado, ficava com um contorno azul — era o outline padrão
do navegador para `<input>` em foco. Diferente dos outros campos do sistema, esse não tinha a
classe `focus:ring` que define o anel verde institucional, então caía no comportamento default do
Chrome/navegador.

### Solução
Adicionado `outline-none focus:ring-2 focus:ring-secondary/30 rounded`, mesmo padrão visual usado
em todo o resto do sistema.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/js/caixa.js` | classe do input de desconto ganhou o anel de foco padrão do sistema |
| `VERSION.txt` | `1.0.0.13` |

### Resultado
Mudança puramente visual (CSS), sem impacto em cálculo ou submit. Build sem avisos.

## [2026-08-18] Caixa: removido também o placeholder "0" do desconto por item (versão 1.0.0.12)

### Problema
A correção anterior deixou o campo "Desc. %" vazio quando o item não tem desconto, mas manteve
`placeholder="0"` como dica visual. O usuário viu esse "0" (cinza, de marca d'água) e achou que
ainda era o mesmo bug de campo pré-preenchido.

### Solução
Removido o `placeholder="0"` também — o campo fica só vazio, sem nenhum texto de fundo.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/js/caixa.js` | `descontoCelulaHtml` não define mais `placeholder` no input de desconto |
| `VERSION.txt` | `1.0.0.12` |

### Resultado
Mudança client-side isolada (renderização de linha), sem impacto no cálculo ou no submit. Build
sem avisos.

## [2026-08-18] Caixa: desconto por item também não nasce mais com "0" (versão 1.0.0.11)

### Problema
No Caixa, o campo "Desc. %" de cada item da venda (`<input type="number">`, já protegido pelo
próprio navegador contra zeros repetidos) ainda nascia com `value="0"` quando o item era
adicionado sem desconto — mesmo padrão de confusão já corrigido em outros campos: usuário digita
por cima do "0" já ali em vez de partir de um campo limpo.

### Solução
Sem desconto, o campo agora nasce vazio (com `placeholder="0"` só como dica visual). O cálculo do
preço do item já tratava `desconto = 0`/vazio como "sem desconto", então não muda nada na conta —
só a experiência de digitar.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/js/caixa.js` | `descontoCelulaHtml` só imprime `value` quando o item já tem desconto |
| `VERSION.txt` | `1.0.0.11` |

### Resultado
Lógica simulada (Node): item novo gera campo vazio; preço sem desconto continua correto; campo
vazio ao recalcular é interpretado como 0 (comportamento já existente, não mudou). App testado
subindo e a tela do Caixa carregando normalmente. Build sem avisos.

## [2026-08-18] Zero à esquerda também restrito no desconto (%) e na quantidade de item (versão 1.0.0.10)

### Problema
A entrada anterior liberou o campo de desconto em modo "%" para aceitar dígitos livres (sem a
máscara monetária), mas sem cortar zeros à esquerda — dava pra digitar "00000000" nesse campo e na
quantidade de item do orçamento/OS (`itemQuantidade`), os únicos dois campos de texto livre restrito
a números do sistema que ainda não tinham essa proteção.

### Solução
Mesma regra já usada em `mascararValor` (telefone/CPF/CNPJ/CEP/valor): zero à esquerda é cortado
enquanto vier seguido de outro dígito (`00005` → `5`, mas um `0` isolado continua `0` — dá pra
apagar o campo e ele fica vazio de novo). Aplicado nos dois campos que faltavam.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/js/orcamento.js` | filtro de zero à esquerda no desconto (modo %) e na quantidade de item |
| `VERSION.txt` | `1.0.0.10` |

### Resultado
Lógica simulada isoladamente (Node): `"00000"` → `"0"`, `"010"` → `"10"`, `"100"` permanece
`"100"` — nos dois campos. Build sem avisos; app testado subindo normalmente após a mudança.

## [2026-08-18] Campo de desconto aplicava máscara de reais mesmo em modo percentual (versão 1.0.0.9)

### Problema
O campo "Desconto" (Orçamento/OS) tinha `data-mascara="valor"` fixo no HTML, independente do tipo
selecionado no `<select>` ao lado (% ou R$). Como "%" é o padrão ao abrir a tela, o campo sempre
nascia formatando como dinheiro — digitar "10" (querendo dizer 10%) virava "0,10", e o usuário via
isso como um "0" fixo/bugado logo ao clicar, porque a máscara monta o valor da direita pra esquerda
(comportamento correto só faz sentido em R$, não em %).

### Solução
O campo agora alterna a máscara de acordo com o tipo escolhido: em "%" fica sem máscara (só
restringe a dígitos, até 3 caracteres — 100% é o teto do cálculo), com placeholder "0"; em "R$"
usa a mesma máscara monetária de sempre, com placeholder "0,00". Trocar o tipo no `<select>` limpa
o campo — um "10" digitado em % não tem o mesmo sentido que "10" em R$ (seria 0,10), então manter
o texto ao trocar confundiria o valor.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Views/Documento/Add.cshtml` | `data-mascara`/`inputmode`/`placeholder`/`value` do campo de desconto passam a depender de `Model.DescontoTipo` |
| `wwwroot/js/orcamento.js` | `atualizarMascaraDesconto()` no `change` do tipo; restrição a dígitos quando sem máscara |
| `VERSION.txt` | `1.0.0.9` |

### Resultado
Simulado o comportamento das duas máscaras isoladamente (Node): digitar "1" depois "0" em modo %
resulta em "10" (não "0,10"); modo R$ preserva o comportamento monetário já existente. Testado via
POST direto simulando o submit com desconto percentual — `Desconto=10.0`, `DescontoTipo=percentual`
persistidos corretamente no banco de teste. Registro de teste removido ao final; banco de dev real
não foi tocado por esta rodada. Build sem avisos.

## [2026-08-18] Campo de estoque mínimo não nasce com "0", garantia vira botões de opção (versão 1.0.0.8)

### Problema 1: campo "Estoque mínimo" nascia preenchido com "0"
Diferente dos outros campos numéricos da mesma tela (Estoque inicial, Estoque máximo, Custo
unitário), o campo "Estoque mínimo" não tinha a guarda condicional "só mostra valor se for um
cadastro existente com valor > 0" — como `EstoqueMinimo` é `int` (não nullable), um cadastro novo
sempre tem valor `0` em C#, e a view imprimia isso literalmente como `value="0"`. Resultado: o
usuário abre a tela para cadastrar um produto novo, o campo já mostra "0", e digitar por cima (ex:
querendo "10") sem apagar primeiro o "0" que já estava lá gera confusão.

**Solução:** mesma guarda que os campos vizinhos já usavam — só imprime o valor quando
`Model.EstoqueMinimo > 0`, senão o campo nasce vazio (com `placeholder="0"` só como dica visual,
que não é um valor real).

### Problema 2: "Garantia do serviço" era um spinner numérico com setas
O campo de dias de garantia (na tela de Orçamento/Ordem de Serviço) era um `<input type="number">`
com `min="90" step="30"` — setas incrementam de 30 em 30, mas a digitação livre continuava aberta,
e o campo já nascia preenchido com "90" (o padrão legal do CDC art. 26, II), com o mesmo problema
de UX do item 1: usuário precisa perceber que já tem um valor ali antes de trocar.

**Solução:** trocado por 4 botões de opção fixos (90 / 120 / 180 / 365 dias) — clicar num deles
marca visualmente e atualiza um campo oculto que vai para o servidor no lugar do spinner. "90 dias"
já vem marcado por padrão (mesmo comportamento de antes, só que agora visível como escolha, não
como número para editar). Sem campo de valor livre — fora dessas 4 opções, o ajuste continua
possível editando o registro depois.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Views/Estoque/Add.cshtml` | guarda condicional no valor inicial de `npEstoqueMinimo` |
| `Views/Documento/Add.cshtml` | campo de garantia trocado de `<input type="number">` para 4 botões + hidden |
| `wwwroot/js/orcamento.js` | clique nos botões de garantia atualiza o hidden e o destaque visual |
| `VERSION.txt` | `1.0.0.8` |

### Resultado
Testado em cópia publicada do banco de dev: cadastro novo de produto mostra o campo de estoque
mínimo vazio; edição de produto existente continua mostrando o valor real salvo. Botões de
garantia renderizam corretamente com "90 dias" pré-selecionado; submit simulando o clique em
"180 dias" (POST direto com `prazoGarantiaDias=180`) confirmou o valor persistido no banco.
Registro de teste removido ao final; banco de dev real não foi tocado por esta rodada. Build sem
avisos.

## [2026-08-18] Tela de Categorias: editar e excluir (versão 1.0.0.7)

### Problema
A entrada anterior desta mesma data unificou a categoria de produto e deu um jeito de **criar**
categoria sem SQL (modal "+" em Pedidos e Estoque/Add), mas não existia nenhuma forma de editar o
nome ou excluir uma categoria — só voltando a editar direto no banco.

### Solução
Nova tela `Categoria/Index` (`GET /Categoria`): lista todas as categorias, quantos produtos (soma
de reposição + estoque) cada uma tem, e se pede marca/modelo. Clicar numa linha abre o mesmo modal
já usado para criar, agora em modo edição (`PUT` semântico via `POST /Categoria/Editar`) — nome
duplicado é bloqueado com mensagem, igual à criação. Excluir (`POST /Categoria/Excluir`) verifica
se a categoria está em uso por algum produto (reposição **ou** estoque) antes de remover — se
estiver, bloqueia com uma mensagem explicando por quê, em vez de deixar o EF Core cascatear a
exclusão (a FK de `Produto.CategoriaId` é obrigatória, sem `SetNull` configurado — excluir sem essa
checagem apagaria produtos de reposição em cascata).

O acesso à tela é um link "Gerenciar categorias" dentro do próprio modal de criação, nas duas telas
que já tinham o botão "+" (Pedidos e Estoque/Add) — abre em aba nova, escolha do usuário para não
perder o formulário em andamento.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Controllers/CategoriaController.cs` | `Index` (lista com contagem de uso), `Editar`, `Excluir` (bloqueia se em uso) |
| `Views/Categoria/Index.cshtml` (novo) | listagem com linha clicável para editar, botão excluir |
| `wwwroot/js/categoria-modal.js` | modo edição no modal compartilhado (título, botão, submit via form real em vez de fetch) |
| `Views/Estoque/Add.cshtml`, `Views/ListaCompra/Index.cshtml` | link "Gerenciar categorias" dentro do modal |
| `VERSION.txt` | `1.0.0.7` |

### Resultado
Testado em cópia publicada do banco de dev real: editar nome (com bloqueio de duplicata), excluir
categoria em uso (bloqueado, categoria com produto de reposição vinculado continuou existindo) e
excluir categoria sem uso (removida). Link "Gerenciar categorias" confirmado presente nas duas
telas de origem. Dados de teste revertidos na cópia ao final; banco de dev real não foi alterado
por esta rodada de testes. Build sem avisos.

## [2026-08-18] Cards clicáveis, categoria de produto unificada, faturamento crítico aos 90% (versão 1.0.0.6)

### Cards/linhas clicáveis em vez de precisar acertar o ícone
Garantia, Serviço, Estoque e Clientes exigiam clicar exatamente no ícone de olho/editar. Agora a
linha inteira é clicável (`cursor-pointer`, navega para Ver/Editar); os controles que continuam
precisando de um clique isolado (excluir, retorno em garantia, registrar movimentação) ganharam
`stopPropagation` para não disparar a navegação da linha junto.

### Estoque: rail lateral inerte removido, seta de voltar removida do Histórico
O `<aside>` fixo do lado direito da listagem de Estoque (exportar, sincronizar, categorias,
etiquetas) não tinha nenhum JS por trás — nem o próprio botão "+" de novo produto funcionava.
Removido por completo (o cadastro de produto já tem outro botão de acesso, fora do rail). A tela
de Histórico de movimentações perdeu a seta "← Estoque": não agrega nada que o botão do navegador
já não resolva.

### Categoria de produto deixa de precisar de SQL
Existiam dois sistemas de categoria: a tabela `Categoria` (só usada pela tela de Pedidos/reposição,
sem nenhuma tela de admin para criá-la — só dava pra popular direto no banco) e um campo texto livre
`ProdutoEstoque.Categoria`, com uma lista fixa (Capinha/Película/Carregador...) hardcoded no HTML do
Estoque. Unificados: `ProdutoEstoque` agora referencia a mesma tabela `Categoria` de Pedidos
(`CategoriaId`, FK com `SetNull` — excluir uma categoria não apaga os produtos, só os deixa sem
categoria). Um botão "+" ao lado do seletor de categoria, em Pedidos e em Estoque/Add, abre um modal
("Nova categoria") que grava via `POST /Categoria/Criar` e adiciona a opção no select na hora, sem
recarregar a página — mesmo endpoint reaproveitado nas duas telas.

**Migração de dados:** produtos que já tinham uma categoria em texto livre foram migrados
automaticamente para a tabela `Categoria` na própria migration (reaproveitando uma categoria
existente de mesmo nome, sem duplicar, e criando uma nova só quando não havia equivalente).
Verificado na cópia de teste e no banco de desenvolvimento real: nenhum produto ficou sem
categoria, nenhuma categoria duplicada.

### Faturamento MEI: fica vermelho aos 90%, não só ao estourar o teto
A barra e o selo de status só ficavam vermelhos depois de ultrapassar 100% do teto; de 80% a 100%
ficavam amarelos o tempo todo, sem sinal de que o limite estava chegando perto. Agora 90%–100%
também é vermelho ("91% do teto — quase lá"), preservando o texto especial para quando realmente
passa do teto. A barra ganhou uma transição suave de largura.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Views/Garantia/Index.cshtml`, `wwwroot/js/garantia.js` | linha clicável (`data-href`), `stopPropagation` nos ícones de ação |
| `Views/Servico/Index.cshtml`, `wwwroot/js/servico.js` | linha clicável, `stopPropagation` no editar/excluir |
| `Views/Estoque/Index.cshtml`, `wwwroot/js/estoque.js` | linha clicável; remoção do `<aside>`/painel de ações lateral e do script órfão `ESTOQUE_EDIT_URL` |
| `Views/Estoque/Historico.cshtml` | remoção da seta "voltar para Estoque" |
| `Views/Cliente/Index.cshtml`, `wwwroot/js/cliente.js` | linha clicável, `stopPropagation` no editar/excluir |
| `Views/Faturamento/Index.cshtml` | limiar de vermelho movido para 90% (`critico`), textos e transição de largura da barra |
| `Models/Categoria.cs`, `Models/ProdutoEstoque.cs` | `ProdutoEstoque.Categoria` (string) → `CategoriaId`/`Categoria` (FK) |
| `Controllers/CategoriaController.cs` (novo) | `POST Criar` — usado pelo modal de nova categoria, deduplica por nome |
| `Controllers/EstoqueController.cs` | `Salvar`/`Index`/`Historico` passam a usar `CategoriaId` |
| `Views/Estoque/Add.cshtml`, `Views/ListaCompra/Index.cshtml` | select de categoria vindo do banco + botão/modal "nova categoria" |
| `wwwroot/js/categoria-modal.js` (novo) | JS do modal, compartilhado pelas duas telas |
| `Data/AppDbContext.cs` | relação `ProdutoEstoque.Categoria` com `OnDelete(SetNull)` |
| `Migrations/20260818140828_AddCategoriaEmProdutoEstoque.cs` | migra dados da coluna texto livre para a FK antes de derrubá-la |
| `VERSION.txt` | `1.0.0.6` |

### Resultado
Testado em cópia do banco de desenvolvimento (produto real com categoria "Película" migrado sem
duplicar), depois aplicado no banco de dev real com backup prévio. Servidor de teste publicado e
logado via curl: todas as 8 telas alteradas responderam 200; `POST /Categoria/Criar` testado criando
categoria nova, reaproveitando duplicata (case-insensitive) e rejeitando nome vazio (400); percentual
do Faturamento forçado a 91% via venda de teste confirmou a barra e o selo ficando vermelhos com o
texto "quase lá". Build sem avisos.

## [2026-08-18] Caixa: venda não gravava, e dropdown de busca vira dropdown de verdade (versão 1.0.0.4)

### Problema 1: venda não salvava no banco, não aparecia em Vendas nem no Fechamento
O botão "Finalizar Venda" tinha **dois** listeners competindo: um `click` no próprio botão (resquício
de uma versão anterior mockada — o texto do toast admitia "exemplo, ainda não gravado no banco de
dados") e o `submit` de verdade no formulário. O `click` dispara antes do `submit`; o handler antigo
zerava `cart.length = 0` e re-renderizava a tabela vazia **antes** do submit rodar, então quando o
form de fato enviava, `itensPost` ficava sem nenhum item — o controller recebia `itemProdutoId` nulo
e recusava a venda com "Adicione ao menos um produto antes de finalizar", sem o usuário perceber que
o carrinho já tinha sido limpo na tela.

**Solução:** removido o handler de `click` fake. Só o `submit` real permanece.

### Problema 2: sugestões de produto num datalist nativo, sem controle de tamanho
A busca de produto usava `<datalist>` do navegador — sem CSS aplicável, largura e estilo fora do
controle do sistema, e sem mostrar preço/estoque de cada opção.

**Solução:** substituído por um dropdown customizado (`#sugestoesProduto`), mesma largura do campo
de busca (`w-full` dentro do `relative`), seguindo o padrão visual já usado no modal de busca de
cliente da OS. Mostra nome, código, saldo em estoque e preço de cada sugestão; navegável por
teclado (setas + Enter), fecha com Escape ou clique fora.

### Problema 3: banner de venda concluída ficava preso na tela
O card verde de sucesso (`TempData["Sucesso"]`, renderizado pelo servidor) não tinha temporizador
— diferente do `#toast` já existente ao lado dele, que some sozinho em 3s. Na frente de caixa a
pessoa continua vendendo sem recarregar a página, então o banner ficava preso indefinidamente.

**Solução:** os três banners de `TempData` (Sucesso/Aviso/Erro) ganharam `id` e um temporizador
de 3s com fade antes de remover o elemento — mesmo comportamento do toast.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/js/caixa.js` | remove o `click` fake; adiciona `buscarProdutos`, dropdown de sugestões, auto-some dos banners de TempData |
| `Views/Caixa/Index.cshtml` | `<datalist>` trocado por `#sugestoesProduto`; `id` nos banners |
| `VERSION.txt` | `1.0.0.4` |

### Resultado
Testado numa cópia do banco real: dropdown mostra sugestões corretas ao digitar (confirmado via
`getComputedStyle`/`getBoundingClientRect` — visível, posicionado, com os itens certos); clicar
numa sugestão adiciona ao carrinho; finalizar a venda grava em `Vendas` e `ItensVenda`, baixa o
estoque do produto (10 → 8 unidades vendendo 2), e aparece corretamente em `/Caixa/Vendas` e no
Fechamento de Caixa do dia (R$ 100,00 em Dinheiro). Banner de sucesso confirmado presente
imediatamente após a venda e ausente 3,5s depois. Build limpo.

---

## [2026-08-18] Painel principal com dado real (versão 1.0.0.5)

### Problema
O Painel (`/`) inteiro era maquete estática desde a origem: "Vendas Hoje" sempre R$ 0,00, "Itens
em Falta" e "Novos Clientes" sempre 0, "Contas a Pagar" sempre R$ 0,00, gráfico de desempenho
semanal sem dado nenhum, "Atividades Recentes" e "Últimos Orçamentos" sempre vazios — mesmo com
vendas, OS, orçamentos e contas já gravados no banco havia meses.

### Solução
`PainelViewModel` novo, montado no `HomeController.Index`:

- **Vendas Hoje** — soma `Venda.Total` do dia, mesma fonte usada no Fechamento de Caixa.
- **Itens em Falta** — conta `ProdutoEstoque` com `Situacao != "ok"` (esgotado ou abaixo do mínimo;
  a propriedade já existia, só não era usada aqui).
- **Novos Clientes** — `Cliente` com `DataCadastro` de hoje.
- **Contas a Pagar** — soma de `ContaAPagar` com `Paga == false`.
- **Desempenho Semanal** — vira um gráfico de barras (CSS puro, sem lib): soma Vendas + pagamentos
  de OS (`PagamentoOrdemServico`, a mesma tabela por trás do Fechamento de Caixa) por dia, últimos
  7 dias. Barra mostra o valor ao passar o mouse.
- **Atividades Recentes** — não existe uma tabela de log de atividades no sistema; o feed é
  composto na hora juntando as últimas Vendas, OS/orçamentos e contas pagas, ordenados por data,
  cada um linkando para a tela de origem.
- **Últimos Orçamentos** — últimos 5 documentos com `Tipo == Orcamento`, com o badge de situação
  (Aberto/Aprovado/Recusado) no mesmo esquema de cor já usado nas outras telas.

Os quatro cards de indicador viraram links para a tela correspondente (Vendas, Estoque, Clientes,
Financeiro) — antes eram só `<div>`, sem destino.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Models/PainelViewModel.cs` | **novo** — `PainelViewModel`, `PontoDesempenho`, `AtividadeRecente` |
| `Controllers/HomeController.cs` | `Index` monta o modelo; `DesempenhoUltimos7Dias`, `AtividadesRecentes` |
| `Views/Home/Index.cshtml` | todos os cards passam a ler `Model` em vez de valor fixo |
| `ROADMAP.md` | nota no item "Dashboards de verdade" — Painel não é mais placeholder, só a tela `/Dashboards` separada continua |
| `VERSION.txt` | `1.0.0.5` |

### Resultado
Testado numa cópia do banco real com uma venda e uma conta a pagar criadas na hora: "Vendas Hoje"
mostrou R$ 145,00 correto, "Contas a Pagar" R$ 800,00, gráfico da semana com a barra de hoje
visível, Atividades Recentes misturando venda/OS/orçamentos por data, tabela de Últimos Orçamentos
com status coloridos batendo com os dados reais (5 orçamentos existentes desde antes desta
sessão). Banco de desenvolvimento não foi tocado — só a cópia. Build limpo.

---

## [2026-08-18] Instalador vira instalador de verdade: clona e atualiza sozinho (versão 1.0.0.3)

### Problema
Dois pontos, descobertos rodando o `install.bat` numa máquina de teste:

1. `dotnet publish` falhava com `Access to the path is denied` ao atualizar. Causa: o projeto já
   tinha sido aberto/buildado por um usuário normal (VS Code) antes do instalador rodar como
   Administrador — os arquivos intermediários em `obj\` ficaram com outro dono, e a build seguinte
   com outro nível de permissão trombava neles.
2. O instalador dependia de a pessoa baixar o repositório inteiro e rodar o script de dentro dele.
   Toda vez que o sistema ganhasse uma funcionalidade nova, quem administra a loja não tinha como
   saber, nem um jeito prático de buscar a atualização sem repetir manualmente o download.

### Solução

**Permissão:** os dois scripts agora apagam `obj\`/`bin\` do projeto antes de publicar, sempre —
elimina esse conflito de permissão de uma vez.

**Instalador de verdade.** `install.sh`/`install.bat` deixaram de depender de baixar o projeto
inteiro: agora é baixar **um arquivo só** e rodar como administrador/root. Ele:

- Instala .NET **e Git** automaticamente se estiverem faltando (Windows: `winget`, com fallback
  para o instalador oficial; Linux: detecta `apt`/`dnf`/`yum`/`pacman`/`zypper`).
- Na primeira vez, clona o repositório numa pasta fixa (`codigo/`, separada de `app/`, onde vai o
  binário publicado — nunca mistura o clone git com o executável).
- Rodando de novo, faz `git fetch --tags` e atualiza para a **última tag publicada**, nunca o
  commit mais recente do `main` direto.

**A trava de tags é a decisão central desta mudança.** Sem ela, "atualiza sozinho com base no meu
repositório" significa que qualquer commit vira produção em todas as lojas instantaneamente, sem
chance de eu testar antes de soltar. Com a trava, eu decido quando uma versão está pronta —
criando uma tag (`git tag vX.Y.Z && git push --tags`) — e só então as máquinas que rodarem o
instalador recebem essa versão. Enquanto o repositório não tiver nenhuma tag, o instalador cai no
HEAD do `main` como reserva.

**Continua não sendo auto-atualização do processo em execução** — decisão já registrada no
`ROADMAP.md` e que não mudou: o app rodando nunca troca os próprios arquivos sozinho. Atualizar
sempre passa por parar o serviço, publicar por cima e subir de novo, o que só o instalador faz.
`loja.db` continua preservado à parte durante o publish.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `install.bat` | reescrito — instala Git, clona/atualiza via tag, interface com passos numerados |
| `install.sh` | idem, para Linux |
| `VERSION.txt` | `1.0.0.3` |

### Resultado
Testado com um repositório git local fazendo o papel do GitHub: primeira instalação clonou e
ficou na tag disponível; criada uma tag nova, uma segunda rodada do instalador buscou e trocou
para ela corretamente (`git fetch --tags` + `--sort=-creatordate`); o banco de dados sobreviveu
intacto ao ciclo completo de "publicar por cima". `dotnet publish` de verdade a partir do clone
git funcionou, com `VERSION.txt` incluído no resultado. Sem tags no repositório, o comando de
listagem não gera erro — cai no fallback do `main` como esperado.

---

## [2026-08-18] Aviso de atualização disponível (versão 1.0.0.2)

### Problema
O instalador (`install.sh`/`install.bat`) roda como serviço do sistema, sozinho, sem terminal
aberto — então quando o sistema ganha uma funcionalidade nova, quem administra a loja não tem
como saber que existe uma versão mais recente para instalar.

### Solução
**Não é atualização automática.** Isso continua descartado no `ROADMAP.md` — o .NET mantém as
DLLs carregadas em memória enquanto o processo roda, e trocar arquivos por baixo dele sem um
supervisor separado arrisca deixar a loja com o sistema quebrado. O que existe agora é só o
**aviso**.

Criado `VERSION.txt` na raiz do repositório, com um número que sobe a cada entrada nova neste
CHANGES.md (formato `1.0.0.X`, decidido em conversa — o quarto número é o contador de mudanças,
comparado numericamente e não como texto, então `1.0.0.9` reconhece corretamente que `1.0.0.12` é
mais novo). Esse arquivo vai junto no `dotnet publish` (referência em `ListasCompras.csproj`).

No Painel, um `fetch` disparado **depois** da página já ter carregado — nunca atrasa a tela
principal — chama `/Home/VerificarVersao`, que compara a versão local contra o `VERSION.txt` da
branch `main` no GitHub (`raw.githubusercontent.com`, público, sem token). Só administrador vê o
aviso, porque só o admin decide rodar o instalador de novo. Sem internet ou GitHub fora do ar, a
checagem falha em silêncio — nunca mostra erro, nunca trava nada, só não mostra o aviso.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `VERSION.txt` | **novo**, na raiz — `1.0.0.2` |
| `ListasCompras/Data/VersaoServico.cs` | **novo** — lê local, busca remota, compara os números |
| `ListasCompras/Controllers/HomeController.cs` | `VerificarVersao`, restrito a Admin |
| `ListasCompras/Program.cs` | `AddHttpClient()` |
| `ListasCompras/Views/Home/Index.cshtml` | banner de aviso + fetch depois do carregamento |
| `ListasCompras/ListasCompras.csproj` | inclui `VERSION.txt` no publish |
| `ROADMAP.md` | nota na entrada "Atualização automática" distinguindo aviso de auto-update |

### Resultado
Build limpo. `VERSION.txt` confirmado dentro da pasta publicada pelo `dotnet publish`. Endpoint
testado logado como Admin: retorna `local`, `remota` e `atualizacaoDisponivel` corretamente;
`remota: null` quando o arquivo ainda não existe no GitHub (não fiz push ainda) — comportamento
esperado, sem aviso até confirmar a versão remota de verdade. Comparação numérica testada
isoladamente: `1.0.0.9` vs `1.0.0.12` reconhece a segunda como mais nova (uma comparação de texto
erraria isso); versões iguais e remota mais antiga corretamente não disparam o aviso.

---

## [2026-08-18] Correção de bugs (máscaras e cadastro de produto)

### Problema 1: total do item de OS calculado errado
Digitar um valor no campo "Valor Unit." de um item da OS (ex: "150,00") fazia o total sair
errado (R$ 15,00 em vez de R$ 150,00). A máscara de valor (`data-mascara="valor"` em
`mascaras.js`) reformata o campo a cada tecla, mas o cálculo do total (`recalcular()` em
`orcamento.js`) tinha seu próprio listener de `input` no mesmo campo — e como os dois escutavam
em fase de bubble, o cálculo rodava **antes** da máscara terminar de reescrever o valor daquele
instante, sempre um dígito atrasado.

**Solução:** o listener da máscara passou a rodar em fase de captura (`capture: true`), que
sempre dispara antes de qualquer listener nos elementos filhos — independente da ordem em que
cada tela liga os próprios scripts.

### Problema 2: valor sem limite de dígitos quebrava o layout
A mesma máscara de valor não tinha teto de dígitos (diferente de telefone, CPF, CNPJ e CEP, que
já cortavam no tamanho certo). Digitar uma sequência grande de zeros gerava um número gigante que
estourava a largura da coluna "Total" e quebrava o layout da tabela de itens.

**Solução:** `mascararValor` agora corta em 10 dígitos — teto de R$ 99.999.999,99, bem acima de
qualquer item real de loja.

### Problema 3: abas do cadastro de produto não trocavam de conteúdo
Na tela **Estoque → Cadastrar novo produto**, clicar nas abas (Características, Imagens, Estoque,
Tributação, Variações) destacava o botão clicado mas o conteúdo nunca mudava — a tela ficava presa
em "Dados básicos" o tempo todo. O botão "Avançar" também não fazia nada.

**Causa:** os painéis de cada etapa usam o atributo `data-step-panel="N"` no HTML, mas
`estoque-add.js` procurava por uma classe `.np-painel` que não existe em lugar nenhum do arquivo —
a lista ficava vazia e `irParaEtapa()` nunca escondia/mostrava painel nenhum. O botão "Avançar"
nunca teve um listener ligado a ele.

**Solução:** o seletor passou a usar `[data-step-panel]`; o botão "Avançar" ganhou a função de
ir para a próxima etapa e some sozinho ao chegar na última.

**Nota de escopo, não bug:** as abas Características, Imagens, Tributação e Variações continuam
sendo só vitrine visual — o modelo `ProdutoEstoque` grava apenas nome, código, categoria, unidade,
preço, custo e estoque (as duas primeiras abas). O texto das próprias abas já avisa isso ("estará
na versão completa do cadastro"); persistir os campos das outras abas é trabalho futuro, a decidir
quando entrar no roadmap.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/js/mascaras.js` | listener em fase de captura + limite de 10 dígitos no valor |
| `wwwroot/js/estoque-add.js` | seletor `[data-step-panel]` + listener do botão "Avançar" |

### Resultado
Testado: digitar "150,00" no valor de um item de OS agora calcula R$ 150,00 corretamente; uma
sequência grande de zeros trava em R$ 99.999.999,99 sem quebrar o layout; navegar pelas 6 etapas
do cadastro de produto — por clique nas abas e pelo botão "Avançar" — mostra o painel certo em
cada uma, e o botão some sozinho na última etapa. Build limpo.

---

## [2026-08-18] Script de instalação (item 1 do roadmap)

### Problema
O público do sistema é dono de loja, não desenvolvedor. Instalar exigia clonar o repositório, ter
o SDK do .NET e rodar `dotnet run` manualmente toda vez que o computador ligasse — sem serviço do
sistema, o Pingo OS parava de rodar no primeiro reinício e ninguém saberia religar.

### Solução
`install.sh` (Linux) e `install.bat` (Windows), na raiz do repositório. Os dois seguem os mesmos
passos: instalam o runtime do .NET se estiver faltando, publicam o sistema numa pasta fixa fora da
pasta baixada (`/opt/pingo-os` / `Arquivos de Programa\PingoOS`), registram como **serviço do
sistema** (systemd / Serviço do Windows — sobe sozinho no boot, reinicia se cair) e abrem o
navegador quando termina.

**Preserva dados em atualização.** Rodar o instalador de novo por cima de uma instalação existente
não pode apagar `loja.db` — o `dotnet publish` limpa a pasta de destino inteira. Os dois scripts
tiram o banco do caminho antes de publicar e devolvem depois. Testado publicando duas vezes seguidas
sobre a mesma pasta: o conteúdo do banco sobreviveu.

**Serviço do Windows de verdade, não gambiarra.** Em vez de depender de uma ferramenta externa
(NSSM) para fingir um serviço, o próprio `Program.cs` ganhou `UseWindowsService()`
(`Microsoft.Extensions.Hosting.WindowsServices`) — o executável sabe se comportar como serviço
quando o `sc create` do `install.bat` o registra assim. No Linux/systemd esse pacote não faz nada,
então não custa nada tê-lo.

**Porta fixa em produção.** Sem isso o Kestrel cairia no padrão 5000, que pode já estar ocupado por
outra coisa na máquina da loja, e o instalador não saberia qual endereço abrir no navegador. Fixado
em `http://localhost:5096` — a mesma porta já usada em desenvolvimento —, só quando nada foi
definido por fora (`ASPNETCORE_URLS` ou `--urls` continuam podendo sobrescrever).

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `install.sh` | **novo** — instalação/atualização como serviço systemd |
| `install.bat` | **novo** — instalação/atualização como Serviço do Windows |
| `ListasCompras/Program.cs` | `UseWindowsService()` + porta fixa em produção |
| `ListasCompras/ListasCompras.csproj` | pacote `Microsoft.Extensions.Hosting.WindowsServices` |
| `README.md` | seção "Para usar na loja" com o passo a passo do instalador |

### Resultado
Build limpo. Testado (sem `sudo`, sem systemd de verdade — só a parte seguinte de publicar):
`dotnet publish` duas vezes seguidas sobre a mesma pasta com o banco preservado entre elas; o
executável publicado com `UseWindowsService()` rodando fora do Windows não gera nenhuma exceção,
sobe normalmente na porta 5096 fixa, e responde 200 nas telas.

---

## [2026-08-14] Financeiro e fechamento de caixa (itens 1 e 1.1 do roadmap)

### Problema
Duas lacunas: (1) nada no sistema registrava entradas e saídas que não são venda nem OS — aluguel,
retirada do dono, compra de material — nem contas com vencimento antes de virarem despesa; (2) não
havia como bater o dinheiro físico da gaveta contra o sistema no fim do dia, porque a OS só guarda
o `Sinal` (haver) e o `Total` como valores únicos, sem dizer **em que dia** cada parte entrou.

O segundo problema é mais sério do que parecia. Numa OS aberta com haver num dia e entregue dias
depois, somar "Total da OS" no dia da entrega contaria o haver de novo; o dia em que o haver foi
deixado não apareceria em fechamento nenhum. Só dava para resolver registrando cada pagamento com
sua própria data — não é possível fazer isso lendo só o que já existia.

### Solução

**`PagamentoOrdemServico`** — um registro por entrada de dinheiro na OS, com data, valor e forma de
pagamento. Dois pontos passaram a criar esses registros:

- `Salvar`: só quando o **sinal aumenta** em relação ao que já estava salvo — editar outra coisa da
  OS (diagnóstico, itens) não recria o haver com a data de hoje, e reduzir o sinal (correção de
  digitação) não vira uma saída de caixa fictícia.
- `AlterarSituacao` para Entregue: registra o saldo que falta, mas só a primeira vez — voltar a
  situação para "Pronta" e marcar "Entregue" de novo não pode cobrar o mesmo saldo duas vezes. A
  checagem não usa `DataEntrega` (que é resetada de propósito ao voltar atrás), e sim se já existe
  um pagamento de origem "Saldo" para aquela OS.

**Fechamento de caixa** (`/FechamentoCaixa`) — Vendas e pagamentos de OS do dia escolhido, cada um
por forma de pagamento, com total geral. As duas fontes ficam **lado a lado, nunca somadas numa
tabela única** — o risco discutido foi o usuário lançar a mesma OS como venda no Caixa "para
aparecer no fechamento", duplicando o que a tela de Faturamento MEI já soma.

**Financeiro** (`/Financeiro`) — lançamentos manuais de entrada/saída com categoria, e contas a
pagar com vencimento. Marcar uma conta como paga gera o lançamento de saída correspondente e
vincula os dois; excluir esse lançamento depois não arrasta a conta junto (senão a dívida
"desapareceria" do controle), mas isso é caso raro e não crítico.

**Sem retroatividade.** As OS que já existiam no banco antes desta mudança não têm pagamento
registrado — o `Sinal` delas é um valor único sem histórico de datas, não dá para reconstruir
quando o haver ou o saldo realmente entraram. Elas não aparecem no fechamento de caixa de dias
passados; só documentos criados ou entregues a partir de agora entram nesse relatório.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Models/PagamentoOrdemServico.cs` | **novo** — data, valor, forma, origem (Haver/Saldo) |
| `Models/LancamentoFinanceiro.cs` | **novo** — `LancamentoFinanceiro` e `ContaAPagar` |
| `Controllers/DocumentoControllerBase.cs` | `Salvar` e `AlterarSituacao` geram os pagamentos |
| `Controllers/FechamentoCaixaController.cs` | **novo** |
| `Controllers/FinanceiroController.cs` | **novo** |
| `Views/FechamentoCaixa/Index.cshtml`, `Views/Financeiro/Index.cshtml` | **novos** |
| `wwwroot/js/financeiro.js` | **novo** — vírgula → ponto nos dois formulários de valor |
| `Views/Home/Index.cshtml` | **Fechamento do dia** dentro do grupo Caixa; **Financeiro** no menu |
| Migration `AddFinanceiro` | três tabelas novas, nenhuma coluna existente tocada |

### Resultado
Testado numa cópia do banco real, logado com o usuário de verdade: criar OS com haver de R$ 200
registrou o pagamento na hora certa; entregar registrou o saldo de R$ 300; alternar Pronta →
Entregue de novo **não duplicou** o saldo (esse era o bug encontrado e corrigido durante o teste,
antes de existir a checagem por pagamento já registrado). Editar a OS aumentando o sinal de R$ 100
para R$ 250 registrou só a diferença (R$ 150); editar sem tocar no sinal não criou nada. Lançamento
manual de entrada e saída, conta a pagar marcada como paga gerando o lançamento de saída — tudo
gravado corretamente. Fechamento de caixa do dia somou R$ 750 batendo com os quatro pagamentos de
teste, Vendas e OS mostradas separadas.

---

## [2026-08-14] Notificação ao cliente por WhatsApp (item 1 do roadmap)

### Problema
Os termos da OS dizem que, para considerar um aparelho abandonado, o cliente precisa ter sido
**notificado por escrito** (cláusula 8, Código Civil art. 1.275, III). O sistema não produzia essa
prova nem tinha como avisar "seu aparelho está pronto" sem o técnico digitar tudo à mão num
aplicativo separado.

### Solução
Decisão de escopo: **link `wa.me`, sem API paga.** Grátis, sem cadastro Business, sem token — troca
automação por simplicidade imediata. A WhatsApp Business API oficial (Meta) e a notificação por
e-mail ficaram anotadas no `ROADMAP.md` como itens futuros — a API exige conta verificada e modelos
de mensagem pré-aprovados, esforço que não se justificava agora.

Botão **"Avisar cliente"** na tela da OS (não aparece no orçamento). Abre um modal com uma mensagem
pré-preenchida — diferente conforme a situação ("pronto para retirada" quando `Situacao == Pronta`,
genérica nos demais casos) — editável antes de enviar. Confirmar grava a notificação no banco **e
só depois** redireciona para `wa.me/55<telefone>?text=<mensagem>` numa nova aba.

**A ordem importa: registra antes de redirecionar.** O sistema não tem como saber se o WhatsApp Web
realmente abriu ou se a mensagem foi enviada — a prova exigida pelos termos é a notificação
registrada no sistema (data, destinatário, texto, quem fez), não uma confirmação de entrega que o
link não oferece. Histórico de avisos fica visível na própria tela da OS.

Telefone normalizado para os dígitos com DDI 55 fixo (mesma suposição de Brasil que o cadastro de
cliente já faz). Botão desabilitado quando o cliente não tem telefone cadastrado.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Models/NotificacaoCliente.cs` | **novo** — canal, destinatário, mensagem, data, quem enviou |
| `Controllers/OrdemServicoController.cs` | `NotificarWhatsApp`: grava e redireciona para `wa.me` |
| `Controllers/DocumentoControllerBase.cs` | `Ver` inclui o histórico de notificações da OS |
| `Views/Documento/Ver.cshtml` | botão, modal de mensagem, seção de histórico |
| `wwwroot/js/notificar-cliente.js` | **novo** — abre/fecha modal, recarrega após enviar |
| `Data/AppDbContext.cs` | `DbSet<NotificacaoCliente>` + mapeamento |
| Migration `AddNotificacaoCliente` | tabela nova, sem dado existente a migrar |

### Resultado
Build limpo. Migration cria só tabela nova, nenhuma coluna existente é tocada.

---

## [2026-08-14] Impressão térmica da OS (item 1 do roadmap)

### Problema
O documento A4 de duas vias está bem resolvido para a assinatura, mas o balcão usa impressora
térmica de 58/80mm no dia a dia — é o comprovante que o cliente leva na hora de deixar o aparelho,
antes de o serviço estar concluído e pronto para o A4 completo.

### Solução
Botão **"Comprovante térmico"** na tela `Ver` da ordem de serviço (não aparece no orçamento — nada
foi deixado fisicamente ainda). Gera um segundo documento, resumido, numa coluna de 74mm: dados da
loja, cliente, aparelho(s), defeito relatado, total previsto, haver e saldo, e uma nota de garantia.
Não repete os termos completos nem duas vias — é comprovante de entrada, não o contrato assinado.

**A4 e térmico nunca saem juntos.** Os dois blocos (`#osImpressao` e `#osTermico`) já vêm prontos
do servidor na mesma página; uma classe no `<body>`, ligada só na hora de clicar o botão, esconde um
e mostra o outro. Usa `@page` nomeado (`page: termica`) para o tamanho 80mm — suportado em
Chrome/Edge/Brave, a base da maioria das impressoras térmicas USB; sem suporte, cai no tamanho A4
padrão (pior encaixe, não quebra a impressão).

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Views/Documento/Ver.cshtml` | `#osTermico`, botão, CSS de impressão nomeada |
| `wwwroot/js/os-impressao.js` | liga/desliga `body.modo-termico` no clique e no `afterprint` |

Nenhuma mudança de modelo — usa os mesmos dados já exibidos no A4. Sem migration.

---

## [2026-08-13] Faturamento MEI (item 2 do roadmap)

### Problema
MEI tem teto anual de faturamento; quem passa sem perceber é desenquadrado do regime e cai numa
carga tributária maior. O sistema já grava vendas e ordens de serviço, mas nada somava isso contra
o teto.

### Solução
Tela nova, **/Faturamento**, com filtro por ano: soma o total das Vendas (Caixa) mais as Ordens de
Serviço **entregues** do ano escolhido, contra o teto de **R$ 81.000** (padrão MEI vigente). Barra
de progresso muda de cor perto do limite (80%) e ao ultrapassar.

**Regime de caixa.** A venda entra na data da venda; a ordem de serviço entra na data de
**entrega**, não na de abertura — é quando o saldo é de fato cobrado. Orçamentos não entram na
soma, porque nada foi recebido ainda.

**Teto fixo por enquanto.** Deixar configurável em `/Configuracao` ficou anotado no `ROADMAP.md`
como melhoria futura, para quando a lei mudar o valor ou a loja precisar de outra faixa — não é
necessário agora.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Controllers/FaturamentoController.cs` | **novo** — soma por ano, `TetoAnualMei = 81_000m` |
| `Views/Faturamento/Index.cshtml` | **novo** |
| `Views/Home/Index.cshtml` | botão **Faturamento MEI** (`trending_up`) no menu |

Nenhuma coluna nova — soma o que já estava gravado. Sem migration.

---

## [2026-08-13] Fotos do aparelho (item 1 do roadmap)

### Problema
Os termos impressos da OS já preveem exclusão de garantia por mau uso, queda e oxidação, mas nada
registrava o estado do aparelho **na entrada**. Sem isso, a cláusula não tem como se defender de
"esse arranhão não estava aí" — é a palavra da loja contra a do cliente.

### Solução
Seção **Fotos do aparelho** na tela `Ver` da ordem de serviço (não no orçamento — o aparelho ainda
não foi entregue). Cada aparelho da OS tem seu próprio conjunto de fotos; escolher um arquivo já
envia, sem botão de confirmação separado.

Guardadas em **disco**, não no banco: `wwwroot/uploads/aparelhos/`, fora do controle de versão
(`.gitignore`). Base64 no banco é como a logo da loja é guardada hoje, e o roadmap já registrava que
"não escalaria para fotos" — várias fotos por aparelho, em várias ordens, infla o `.db` rápido e
pesa em todo backup.

Validado no servidor, não só no `accept` do input: apenas JPEG/PNG/WEBP, até 8 MB. Nome do arquivo
em disco é um GUID novo — o nome original do upload não é confiável (pode ter caminho, caracteres
especiais) nem é guardado.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `Models/FotoAparelho.cs` | **novo** — arquivo, data de envio, pertence a um `AparelhoOs` |
| `Data/FotoAparelhoServico.cs` | **novo** — validação de tipo/tamanho, salvar e remover do disco |
| `Controllers/OrdemServicoController.cs` | `EnviarFoto` e `ExcluirFoto`, restritos a ordens (não orçamentos) |
| `Controllers/DocumentoControllerBase.cs` | `Ver` inclui `Aparelhos.Fotos` |
| `Views/Documento/Ver.cshtml` | seção de fotos, fora da área de impressão |
| `wwwroot/js/fotos-aparelho.js` | **novo** — envia ao escolher arquivo, exclui com confirmação |
| `.gitignore` | `ListasCompras/wwwroot/uploads/` |
| Migration `AddFotosAparelho` | tabela nova, sem dado existente a migrar |

Exclusão de foto e de arquivo em disco são independentes de propósito: se o arquivo já sumiu do
disco por algum motivo, o registro ainda sai do banco — o inverso nunca deixaria lixo órfão sem
alguém notar, mas travar a exclusão por um arquivo ausente seria pior.

### Resultado
Build limpo. Migration cria só a tabela nova, nenhuma coluna existente é tocada.

---

## [2026-08-12] Voltar do navegador não reabre o formulário salvo

### Problema
Salvar uma OS/orçamento leva para a tela seguinte (`Ver`) e empilha o formulário `Add` no
histórico do navegador. Clicar em "voltar" reabria o formulário já enviado em vez de ir para a
listagem — comportamento padrão de qualquer submit HTML, não um bug de rota.

### Solução
`wwwroot/js/orcamento.js`: o envio do formulário passou a ser por `fetch` seguido de
`location.replace(r.url)`. `replace` troca a página atual no histórico em vez de empilhar uma
nova, então o `Add` some do histórico e "voltar" a partir da tela seguinte cai na listagem.

Mesmo padrão nos outros três formulários de cadastro (`Add` → salva → `Index`), que tinham o
mesmo problema.

### Arquivos Alterados
| Arquivo | Alteração |
|---|---|
| `wwwroot/js/orcamento.js` | submit por `fetch` + `location.replace` |
| `wwwroot/js/servico-add.js` | idem |
| `wwwroot/js/estoque-add.js` | idem |
| `Views/Cliente/Add.cshtml` | idem, id `formCliente` + script inline (não tinha JS próprio) |

---

## [2026-08-12] Orçamento separado da Ordem de Serviço

### Problema
A tela chamada "Orçamento" produzia, na verdade, uma ordem de serviço completa: termos legais,
assinatura das duas partes, garantia de 90 dias e duas vias. Mas nem todo atendimento é um aparelho
deixado na loja. A maior parte é uma pergunta de balcão — "quanto custa a frontal desse celular?" —
e para responder isso o sistema obrigava a emitir um documento que promete garantia sobre um serviço
que ninguém autorizou e que talvez nem seja feito.

O efeito colateral era pior que a burocracia: um papel assinado prometendo 90 dias de garantia,
entregue a quem só perguntou o preço.

### Solução
Dois documentos, dois botões na tela inicial, um caminho entre eles.

| | Orçamento | Ordem de serviço |
|---|---|---|
| Numeração | `ORC-000001` | `OS-000001` |
| Situações | Aberto → Aprovado / Recusado | Aberta → Pronta → Entregue |
| Vias impressas | 1 | 2, com linha de corte |
| Termos e assinatura | não | sim |
| Garantia | não | 90 dias, da retirada |
| Haver, forma de pagamento, parcelamento | não | sim |
| Validade | 10 dias (ajustável) | não vence |

**Aprovar é um botão.** No orçamento aberto, "Cliente aprovou — gerar OS" cria a ordem já com o
mesmo cliente, aparelhos, diagnóstico, itens e desconto; o orçamento fica gravado como *Aprovado* e
as duas telas passam a se referenciar (`OrcamentoOrigemId`). Aprovar duas vezes não gera duas
ordens — a segunda vez leva para a ordem que já existe.

Digitar tudo de novo seria o mesmo que não ter orçamento: ninguém usaria.

**Não é tabela nova.** Orçamento e ordem compartilham cliente, aparelhos, itens e contas — duplicar
isso significaria duplicar também toda correção futura. O que os separa é um campo `Tipo`, e o que
muda de comportamento está em um lugar só: `DocumentoControllerBase`. `OrcamentoController` e
`OrdemServicoController` são cascas de poucas linhas que fixam o próprio tipo. As três telas
mudaram de `Views/Orcamento/` para `Views/Documento/` e servem aos dois.

Cada rota só enxerga o seu tipo: abrir `/Orcamento/Ver/1` numa ordem de serviço dá 404, e o
contrário também. Situação inválida é recusada no servidor — um orçamento não pode ser marcado
"Entregue" nem uma ordem "Aprovado". No orçamento, haver e parcelamento são ignorados mesmo que
cheguem no formulário: os campos continuam no HTML (o script de cálculo conta com eles), só ficam
escondidos, e o servidor não confia neles.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Controllers/DocumentoControllerBase.cs` | **novo** — toda a lógica compartilhada |
| `Controllers/OrcamentoController.cs` | reescrito: tipo Orçamento + `GerarOrdem` |
| `Controllers/OrdemServicoController.cs` | **novo** — tipo OS + `RetornoGarantia` |
| `Views/Documento/` | era `Views/Orcamento/`; as três telas com o que muda por tipo |
| `Models/OrdemServico.cs` | `Tipo`, `ValidadeDias`, `Validade`, `OrcamentoOrigemId`, `SituacoesOrcamento` |
| `Views/Home/Index.cshtml` | botão **Ordem de serviço** (`receipt_long`) no menu |
| `Views/Garantia/Index.cshtml` | links apontando para `OrdemServico` |
| `wwwroot/js/os-impressao.js` | pula o clone da 2ª via quando não há `#osVia2` |
| `wwwroot/js/os-lista.js` | o nome do que se conta vem do HTML ("1 orçamento" / "1 ordem") |
| Migration `SepararOrcamentoDeOrdemServico` | as três colunas novas |

A migration classifica tudo que já existe como `OrdemServico` — via `defaultValue` **e** um `UPDATE`
explícito. O padrão declarado no C# só vale para objeto novo; linha que já está no banco precisa do
UPDATE. Foi exatamente o que faltou quando a garantia entrou zerada nas ordens antigas.

### Resultado
Testado numa cópia do banco real: as 3 ordens existentes migraram como `OrdemServico` com itens,
aparelhos e garantia intactos, e continuam aparecendo em `/OrdemServico` e em `/Garantia`. Um
orçamento novo saiu como `ORC-000001` ignorando o haver, o parcelamento e a garantia enviados no
formulário; aprovado, gerou a `OS-000004` com cliente, aparelho e item copiados, e a segunda
aprovação levou à mesma ordem em vez de criar outra. Cruzar as rotas dá 404, situação inválida é
recusada, e a impressão foi conferida nas duas: orçamento em via única com "Condições", ordem com
as duas vias, termos e assinaturas.

---

## [2026-08-11] Catálogo de serviços (item 1 do roadmap)

### Problema
Toda ordem de serviço tinha a descrição do item digitada do zero. "Troca de tela", "troca de telaa",
"TROCA DE TELA" e "troca tela + película" eram, para o sistema, quatro serviços diferentes — o que
inviabiliza qualquer relatório de "o que mais se faz aqui". Pior: o preço saía da memória de quem
estava no balcão, então o mesmo serviço saía por R$ 250 numa OS e R$ 300 na seguinte.

### Solução
Um cadastro em **/Servico** (listar, adicionar, editar, excluir) **e** um seletor dentro da ordem de
serviço. Foi o ponto que discutimos antes de começar: catálogo que não alimenta a OS é cadastro que
ninguém usa — o técnico continuaria digitando à mão e o cadastro envelheceria sozinho.

- Cada serviço tem nome, categoria (com sugestões: Tela, Bateria, Placa, Software…), valor padrão,
  descrição e a marca **Em oferta**. Serviço que a loja parou de fazer é desmarcado, some do seletor
  da OS e continua no histórico — não se apaga o passado para mudar o presente.
- Na OS, cada linha de item ganhou um botão de ferramenta que abre o catálogo, com busca por nome ou
  categoria. Escolher preenche descrição e valor.
- O valor do catálogo é **sugestão**: fica editável na linha. Negociar desconto num atendimento não
  pode exigir alterar o preço de tabela.
- Continua sendo possível digitar item livre — o catálogo não vira obrigação.

**Excluir um serviço não mexe em ordem nenhuma.** Os itens da OS guardam descrição e valor como
texto, não como referência ao catálogo. Uma OS impressa e assinada em março tem que continuar
dizendo o que dizia, mesmo que a loja mude o preço ou pare de oferecer o serviço em agosto.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Models/Servico.cs` | **novo** — nome, categoria, valor padrão, descrição, `Ativo` |
| `Controllers/ServicoController.cs` | **novo** — Index, Add, Salvar, Excluir e `Buscar` (JSON) |
| `Views/Servico/Index.cshtml`, `Add.cshtml` | **novos** — lista com filtro e formulário |
| `wwwroot/js/servico.js`, `servico-add.js` | **novos** — filtro da lista e vírgula → ponto no envio |
| `Views/Orcamento/Add.cshtml` | modal `#modalServico` do seletor |
| `wwwroot/js/orcamento.js` | botão do catálogo nas linhas de item e busca no modal |
| `Views/Home/Index.cshtml` | botão **Serviços** no menu lateral (ícone `handyman`) |
| `Data/AppDbContext.cs` + migration `AddServicos` | tabela `Servicos` |

O `Buscar` usa `EF.Functions.Like` (o `Contains` vira `instr()` no SQLite, que diferencia maiúscula)
e devolve só os ativos. O valor chega como texto e é lido com `CultureInfo.InvariantCulture`, como
no resto do sistema — o mesmo cuidado que evitou o bug de R$ 620 virar R$ 62.000.

### Resultado
Testado numa cópia do banco real: 4 serviços cadastrados, o inativo não aparece em `/Servico/Buscar`,
busca por "tela" encontra "Troca de tela", o seletor preenche descrição e valor na linha da OS, e
excluir um serviço deixou intactos os 3 itens das ordens já existentes.

---

## [2026-08-11] Backup e restauração pela tela de Configuração (item 2 do roadmap)

### Problema
O banco guarda clientes, ordens de serviço, estoque, vendas e usuários — e o backup dependia de a
pessoa saber copiar `loja.db` pelo terminal. Na prática, não acontecia.

### Solução
Dois botões em **/Configuracao**, visíveis só para administradores.

**Baixar** usa o `VACUUM INTO` do SQLite em vez de copiar o arquivo. Copiar o `.db` na mão deixaria
de fora o que ainda está no journal **WAL**; o `VACUUM INTO` gera um arquivo já consolidado e
íntegro. O nome sai como `pingo-os-<loja>-<data-hora>.db`.

**Restaurar** tem três travas, porque é a operação mais destrutiva do sistema:

1. Exige digitar `RESTAURAR` — nome de arquivo errado num clique não apaga a loja.
2. Valida que o arquivo é um banco do Pingo OS (procura `OrdensServico`, `Clientes`, `Usuarios` e
   `__EFMigrationsHistory`), recusando qualquer outro SQLite.
3. Guarda o banco anterior ao lado do atual como `loja.db.antes-da-restauracao-<data>` — restaurar
   o arquivo errado ainda tem volta.

Antes de trocar o arquivo, o serviço fecha a conexão e chama `SqliteConnection.ClearAllPools()`;
sem isso o arquivo fica travado e a troca falha (no Windows, sempre). Os arquivos `-wal` e `-shm`
do banco antigo são apagados — deixá-los corromperia o restaurado. Ao final a sessão é encerrada,
porque o usuário logado pode não existir no backup.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Data/BackupServico.cs` | **novo** — `GerarCopia`, `EhBancoValido`, `Restaurar` |
| `Controllers/ConfiguracaoController.cs` | ações `Backup` e `Restaurar`, restritas a Admin |
| `Views/Configuracao/Index.cshtml` | seção de backup com os dois cartões |

### Resultado
Testado numa cópia do banco real: download de 184 KB com 3 ordens, 2 clientes e 1 usuário; upload
de um arquivo de texto recusado ("não é um banco SQLite válido"); confirmação errada recusada sem
tocar nos dados; restauração de verdade desfazendo um cliente criado depois do backup (3 → 2), com
a cópia de segurança gravada e a sessão encerrada. Depois de restaurar, login e as seis telas
principais responderam 200.

---

## [2026-08-11] Garantias (item 1 do roadmap)

### Problema
A OS impressa promete 90 dias de garantia e o sistema não guardava nada disso. O cliente voltava
dizendo "está na garantia" e não havia como conferir a data, o que foi trocado, nem se o mesmo
defeito já tinha voltado antes. Era um laço que nós mesmos abrimos.

### Solução
A garantia não virou tabela nova: ela **é** a OS depois da entrega. O que faltava eram três campos
e uma tela.

- `PrazoGarantiaDias` na OS, padrão **90** (mínimo do CDC, art. 26, II). A loja pode prometer mais,
  nunca menos — o controller trava com `Math.Max`.
- A garantia **conta da `DataEntrega`**, não da abertura. Enquanto o aparelho não foi retirado, ela
  nem começou.
- `OrdemOrigemId` liga um **retorno em garantia** à ordem original.

Tela `/Garantia` lista as ordens entregues com dias restantes, vencimento, o que foi feito e quantos
retornos cada uma teve. O botão de retorno abre uma OS nova já com cliente e aparelhos preenchidos e
o diagnóstico começando em "Retorno em garantia da OS-XXXXXX".

O documento impresso ganhou um bloco **Garantia** com o prazo e a data de validade — antes o cliente
levava só a promessa genérica de 90 dias no meio dos termos.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Models/OrdemServico.cs` | `PrazoGarantiaDias`, `OrdemOrigemId` e as contas (`GarantiaFim`, `GarantiaVigente`, `DiasDeGarantiaRestantes`, `SituacaoGarantia`) |
| `Controllers/GarantiaController.cs` | **novo** — listagem com a contagem de retornos |
| `Controllers/OrcamentoController.cs` | prazo no `Salvar` e a ação `RetornoGarantia` |
| `Views/Garantia/Index.cshtml`, `wwwroot/js/garantia.js` | **novos** |
| `Views/Orcamento/Add.cshtml` | campo de prazo e aviso de retorno |
| `Views/Orcamento/Ver.cshtml` | selo de garantia, link do retorno e bloco no papel |
| `Views/Home/Index.cshtml` | "Garantias" na lateral, depois de Orçamento |
| Migration `AddGarantiaOs` | duas colunas |

### Bug encontrado nos testes
As ordens já existentes ficaram com **garantia de 0 dias**: o `AddColumn` do EF usa o padrão do tipo
(0), e o valor padrão do modelo C# só vale para objetos novos, não para linhas já gravadas. Isso
contrariaria o que a OS delas prometeu impresso. Corrigido com `defaultValue: 90` mais um `UPDATE`
de backfill na própria migração.

### Resultado
Testado numa cópia do banco real: as 3 ordens existentes migraram com 90 dias; antes da entrega a
tela mostra o estado vazio correto; marcar como entregue em 11/08 fez a garantia valer até 09/11;
o retorno abriu com cliente e aparelho herdados, gravou ligado à origem e passou a ser contado como
"1 retorno" na listagem.

---

## [2026-08-11] Cancelar da OS ia para o painel; botão de aparelho e assinatura

### Problema
O "Cancelar" do formulário de ordem de serviço apontava para `Home/Index` — largava o usuário no
painel em vez de devolvê-lo à lista de ordens. Era o único formulário do sistema com esse destino:
Cliente, Estoque e Novo usuário já voltavam para a própria listagem.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Views/Orcamento/Add.cshtml` | Cancelar → `Orcamento/Index`; botão de aparelho ganhou o rótulo "Adicionar aparelho", no mesmo formato do "Adicionar Item" |
| `Views/Orcamento/Ver.cshtml` | a linha de assinatura do cliente mostra **o nome dele** em vez do rótulo genérico |

Na OS impressa as duas assinaturas ficam simétricas: de um lado o nome do cliente, do outro o do
técnico que emitiu — cada um assina sobre o próprio nome.

### Sobre o "Salvar"
Verificado: salvar leva para `Orcamento/Ver/{id}` — a própria ordem recém-gravada, dentro do
módulo, não para o painel. É de lá que se imprimem as duas vias. Se preferir cair na listagem em
vez da OS, é uma linha no controller.

---

## [2026-08-11] Correção: 2ª via da OS saía sem formatação

### Problema
Depois da mudança para vários aparelhos, a impressão quebrou: a **1ª via saía normal e a 2ª saía
como texto corrido**, sem tabelas nem colunas.

### Causa
Ao trocar o bloco de aparelho único pelo laço de vários aparelhos, sobrou um `</div>` da marcação
antiga. Esse fechamento a mais encerrava o `#osVia1` cedo demais e, em cascata, o próprio
`#osImpressao`. Como **todo o CSS de impressão é escopado em `#osImpressao ...`**, a 2ª via — que o
JavaScript insere em `#osVia2` — passava a ficar fora desse escopo e não recebia estilo nenhum.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Views/Orcamento/Ver.cshtml` | `</div>` sobrando removido do bloco de aparelhos |

### Resultado
Balanço de `<div>`/`</div>` conferido (52 para 52 a partir da moldura) e PDF regerado: **uma folha
A4, duas vias formatadas**, com a linha de corte entre elas e o bloco de pagamento em ambas.

---

## [2026-08-11] Vários aparelhos por OS e bloco de pagamento

### Problema
Dois casos reais que a tela não cobria: o cliente que deixa **mais de um aparelho** na mesma visita
(abrir uma OS por aparelho separaria o que é um atendimento só) e o cliente que deixa um **haver**
— adiantamento — que só cabia como texto solto no diagnóstico. Desconto, forma de pagamento e
parcelamento também não existiam em lugar nenhum.

### Solução

**Aparelhos** viraram uma tabela própria (`AparelhoOs`), com botão **+** no cabeçalho da seção.
Limite de **5 por ordem**: não é técnico, é o que cabe nas duas vias impressas na mesma A4 sem a
escala automática deixar o texto ilegível.

**Pagamento** ganhou seção própria, com as contas explícitas na tela e no papel:

```
Subtotal dos itens          R$ 800,00
Desconto (10%)            − R$  80,00
Haver deixado pelo cliente− R$ 200,00
Falta pagar                 R$ 520,00     →  3x de R$ 173,33
```

Desconto aceita **% ou R$** (seletor ao lado do campo), forma de pagamento cobre dinheiro, PIX,
débito, crédito e "a combinar", e o parcelamento mostra o valor da parcela enquanto se digita.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Models/AparelhoOs.cs` | **novo** |
| `Models/OrdemServico.cs` | aparelhos em coleção; campos de pagamento; contas (`Subtotal`, `DescontoEmReais`, `Total`, `SaldoAPagar`, `ValorParcela`) |
| `Controllers/OrcamentoController.cs` | recebe aparelhos em array e os dados de pagamento |
| `Views/Orcamento/Add.cshtml` | botão **+**, blocos de aparelho dinâmicos, seção Pagamento com resumo ao vivo |
| `Views/Orcamento/Ver.cshtml` | uma linha por aparelho; subtotal, desconto, haver e falta pagar; bloco Pagamento |
| `Views/Orcamento/Index.cshtml` | coluna Aparelho mostra "Galaxy A54 +1" quando há mais de um |
| `wwwroot/js/orcamento.js` | blocos de aparelho e cálculo do pagamento |
| Migration `AparelhosEPagamentoOs` | tabela nova + colunas de pagamento |

### A migração preserva dados
O scaffold do EF gerou os `DropColumn` **antes** do `CreateTable`, o que apagaria os aparelhos já
cadastrados. Reordenei: cria a tabela, copia os dados com `INSERT ... SELECT`, e só então remove as
colunas antigas. Testado numa cópia do banco real: as duas ordens existentes tiveram seus aparelhos
migrados intactos.

### Detalhe que evitou um bug
O campo de número de série usa `readonly`, não `disabled`, quando "Sem número" está marcado. Campo
desabilitado não é enviado no formulário, e isso desalinharia os arrays de aparelhos no servidor —
o aparelho 2 receberia a série do 3.

### Resultado
Testado numa cópia do banco real: OS com 2 aparelhos (um sem número de série), 2 itens somando
R$ 800,00, 10% de desconto e R$ 200,00 de haver resultou em **R$ 520,00 a pagar em 3x de R$ 173,33**
— conta conferida na tela, no banco e no PDF, que continua saindo em **uma folha A4** com as duas
vias.

---

## [2026-08-10] Ordem de serviço editável

### Problema
Depois de salva, a OS era imutável: um dado faltando ou digitado errado só se resolvia excluindo e
refazendo — o que queimaria o número sequencial e apagaria o histórico.

### Solução
`Add` passou a aceitar um `id` e servir para criar **e** editar, como já fazem Cliente e Estoque.
A tela reabre com tudo preenchido: cliente selecionado, dados do aparelho, diagnóstico e a lista de
itens com seus valores. `Salvar` distingue os dois casos pelo `id`.

Ao editar, **os itens são substituídos** pelos que vierem do formulário — remover uma peça é apagar
a linha, e o que sai do formulário é o que fica gravado.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Controllers/OrcamentoController.cs` | `Add(int? id)` carrega a OS; `Salvar` atualiza quando `id > 0` |
| `Views/Orcamento/Add.cshtml` | vira criar/editar: campos preenchidos, itens vindos do servidor, título e botão mudam |
| `Views/Orcamento/Index.cshtml`, `Ver.cshtml` | botão de editar |
| `wwwroot/js/orcamento.js` | não cria linha em branco quando já há itens; o cliente já vem escolhido |

**O que a edição preserva:** número sequencial, situação, data de abertura e autoria. Só os dados
do serviço mudam.

### Ressalva
A OS impressa é assinada pelo cliente. Editar depois da assinatura faz o sistema divergir do papel
que está com ele — útil para corrigir erro de digitação, arriscado para mudar valor ou peça de uma
ordem já entregue. Não há trava para isso; fica ao critério de quem usa.

### Resultado
Testado: a tela de edição abre com cliente, aparelho, diagnóstico e itens preenchidos; a alteração
gravou modelo, número de série, diagnóstico e dois itens novos; número, situação e data de abertura
seguiram intactos e nenhum item ficou órfão.

---

## [2026-08-10] Situação da OS editável em qualquer direção

### Problema
Marcar uma ordem como **Entregue** era irreversível. A lista só mostrava "marcar como pronta"
enquanto estava Aberta e "marcar como entregue" enquanto não estivesse entregue — depois disso não
havia botão nenhum. Quem clicasse por engano ficava sem saída pela interface.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Views/Orcamento/Index.cshtml` | a coluna Situação virou um seletor que grava ao mudar; a linha inteira abre a OS ao ser clicada |
| `Views/Orcamento/Ver.cshtml` | mesmo seletor no cabeçalho; link "voltar" removido |
| `Controllers/OrcamentoController.cs` | `AlterarSituacao` aceita qualquer transição e um `retorno` para voltar à tela de origem |
| `wwwroot/js/os-lista.js` | `abrirOs()` — o clique na linha ignora cliques em controles (seletor, botões, links) |

**Sobre a data de entrega:** sair de Entregue **limpa** a `DataEntrega`. Se foi marcada por engano,
não faz sentido a garantia de 90 dias seguir contando daquele dia. Voltar para Entregue grava a
data do momento, e alterações que mantêm a situação preservam a data original (`??=`).

### Resultado
Testado o caminho que estava travado: Entregue → Aberta funciona e a data de entrega é zerada.
Ida e volta completa (Aberta → Entregue → Pronta → Entregue) mantém a coerência, e alterar pela
tela Ver retorna para a Ver em vez da listagem.

---

## [2026-08-07] Caixa vira grupo na lateral, com Frente de caixa e Vendas

### Problema
A tela de Vendas só era alcançável por um botão dentro da própria Frente de caixa, o que a fazia
parecer um anexo do caixa em vez de uma tela própria. E "Caixa" na lateral levava direto para a
frente de caixa, sem revelar que existia outra coisa ali dentro.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Views/Home/Index.cshtml` | "Caixa" deixou de ser link e virou botão de grupo: começa fechado e o clique revela **Frente de caixa** e **Vendas** indentados abaixo |
| `Views/Caixa/Index.cshtml` | botão "Vendas" removido da barra de ferramentas |

Sem seta de expandir, por escolha do autor — o próprio rótulo é o gatilho. O botão carrega
`aria-expanded` para quem navega por leitor de tela saber que ali há conteúdo recolhido.

---

## [2026-08-07] Estoque e Caixa no banco, com a venda baixando o estoque

### Problema
Os dois módulos eram **duas telas que não se falavam**. O Estoque guardava os produtos no
`localStorage` do navegador — sumiam ao limpar os dados, não existiam em outra máquina e o `loja.db`
não tinha produto nenhum. O Caixa vendia uma lista de **8 produtos escritos à mão no controller**, e
o botão "Finalizar Venda" não gravava nada: a venda evaporava. Vender não mexia no estoque.

### Solução
Quatro entidades novas e um serviço que centraliza o saldo:

- **`ProdutoEstoque`** — separado do `Produto` da Lista de compras, que é genérico ("Capinha
  Silicone") e se combina com um modelo. Este é item de prateleira, com código e preço.
- **`MovimentacaoEstoque`** — entrada/saída com motivo, data e autor. **O saldo deixou de ser um
  número que alguém digita e passou a ser a soma do histórico**, inclusive o saldo inicial do
  cadastro, que entra como movimentação.
- **`Venda` + `ItemVenda`** — número sequencial, forma de pagamento, desconto por item e autor.
- **`EstoqueServico`** — único lugar que altera saldo. Ajuste manual e venda passam por ele, então
  nenhuma mudança escapa do histórico.

**A ligação que faltava:** finalizar a venda gera automaticamente as saídas de estoque, com o
número da venda no motivo. Sem saldo, o sistema avisa mas deixa concluir e o saldo fica negativo —
travar a venda com o cliente no balcão é pior, e o negativo é o sinal de que a prateleira e o
sistema divergiram.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Models/ProdutoEstoque.cs`, `Models/Venda.cs` | **novos** — produto, movimentação, venda e itens |
| `Data/EstoqueServico.cs` | **novo** — movimentação e numeração sequencial |
| `Controllers/EstoqueController.cs` | reescrito — Index, Add, Salvar, Movimentar, Historico, Excluir, Buscar |
| `Controllers/CaixaController.cs` | reescrito — produtos do banco, Finalizar, Vendas |
| `Views/Estoque/Index.cshtml` | tabela e resumo renderizados pelo servidor; modal virou formulário |
| `Views/Estoque/Historico.cshtml`, `Views/Caixa/Vendas.cshtml` | **novas** telas |
| `wwwroot/js/estoque.js` | de 517 para 95 linhas — sobrou filtro e modal |
| `wwwroot/js/estoque-add.js`, `caixa.js` | postam para o servidor com decimais em ponto |
| `Models/EstoqueIndexViewModel.cs`, `CaixaIndexViewModel.cs` | removidos — viraram entidades |
| Migration `AddEstoqueEVendas` | quatro tabelas novas |

Excluir um produto **não apaga o histórico de vendas**: o item guarda código, descrição e preço
praticado, então a venda antiga continua legível (`DeleteBehavior.SetNull`).

Dois códigos que vinham do relógio do navegador viraram sequência do servidor: produto
(`P-000001`) e venda (`V-000001`).

### Bug encontrado nos testes
As mensagens de sucesso e aviso não apareciam no Caixa: o bloco de `TempData` não tinha sido
inserido, porque a tela do Caixa não tem `<main>` e meu ponto de ancoragem não casou. Inserido no
lugar certo e verificado.

### Resultado
Testado de ponta a ponta: 3 produtos cadastrados com preços corretos (R$ 39,90 / 24,90 / 49,90 —
sem o erro de multiplicar por 100), código sequencial e manual convivendo, saldo inicial virando
movimentação, entrada e saída manuais, e uma venda de 2 capinhas com 10% de desconto mais 1
carregador fechando em **R$ 121,72** — exatamente a conta certa. O estoque baixou de 7 para 5 e de
6 para 5, com as saídas ligadas à venda `V-000001`. Venda acima do saldo deixou o produto em −5 com
o aviso na tela. Excluir um produto vendido manteve os itens da venda no histórico.

---

## [2026-08-07] Seta dos selects sobre o texto, ordem da sidebar e voltar da nova OS

### Problema
Em **10 selects** de 5 telas a seta do menu suspenso caía em cima do texto ("Todas as situações⌄").
Causa: o plugin `forms` do Tailwind desenha a seta como fundo e reserva `padding-right: 2.5rem`
para ela, mas a classe `px-md` vinha depois e sobrescrevia esse espaço com 16px.

### Arquivos Alterados

| Alteração | Alcance |
|---|---|
| `px-md` → `pl-md pr-10` em `<select>` | 10 selects em Orcamento, Estoque (Index e Add), Home e Conta/NovoUsuario |
| Link "voltar" removido | `Views/Orcamento/Add.cshtml` |
| Ordem da sidebar | Caixa → Orçamento → Estoque → Clientes → Lista de compra → Dashboards |

A ordem nova segue o dia da loja: primeiro o que se usa a toda hora (venda e ordem de serviço),
depois consulta (estoque, clientes), e por último o que se abre de vez em quando.

---

## [2026-08-07] Ordem de Serviço no banco (item 1 do roadmap)

### Problema
O Orçamento montava a OS na tela e perdia tudo ao sair. O número da ordem vinha do relógio no
JavaScript (`"OS-" + Date.now().slice(-6)`), então **cada impressão gerava um número diferente para
a mesma ordem** — e nenhum deles existia em lugar nenhum depois.

### Solução
Entidades `OrdemServico` e `ItemOrdemServico`, referenciando `ClienteId` (não copiando os dados do
cliente, conforme a ordem que o roadmap definiu). O módulo passou a seguir a convenção das outras
telas: `Index` lista, `Add` é o formulário, `Ver` mostra e imprime.

- **Numeração sequencial** de verdade: `OS-000001`, `OS-000002`, gerada no servidor.
- **Situações** Aberta → Pronta → Entregue. Marcar como entregue grava `DataEntrega`, que é de onde
  a garantia de 90 dias passa a contar.
- **Autoria** (item 4 do roadmap antigo, feito junto): a OS grava quem a emitiu e o nome aparece na
  linha "Responsável Técnico" do documento impresso. Custava um campo e re-migrar depois seria pior.
- **Impressão só de OS salva.** O botão saiu do formulário e ficou na tela `Ver` — imprimir com
  número falso era exatamente o problema.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Models/OrdemServico.cs` | **novo** — OS, itens e as constantes de situação |
| `Controllers/OrcamentoController.cs` | reescrito — Index, Add, Ver, Salvar, AlterarSituacao, Excluir |
| `Views/Orcamento/Index.cshtml` | **nova** — listagem com busca e filtro por situação |
| `Views/Orcamento/Add.cshtml` | o antigo formulário, agora postando de verdade |
| `Views/Orcamento/Ver.cshtml` | **nova** — documento preenchido pelo servidor + botão de imprimir |
| `wwwroot/js/os-lista.js`, `os-impressao.js` | **novos** — filtro da lista e clonagem da 2ª via |
| `wwwroot/js/orcamento.js` | impressão removida; `clienteId` no post; validação antes de enviar |
| `Controllers/ClienteController.cs` | excluir cliente com OS agora explica em vez de estourar |
| Migration `AddOrdensServico` | duas tabelas novas |

Apagar um cliente com histórico é bloqueado no banco (`DeleteBehavior.Restrict`); apagar um usuário
deixa a OS intacta (`SetNull`).

### Bug sério encontrado nos testes: preços multiplicados por 100
Salvar um item de **R$ 620,00 gravava R$ 62.000,00**. O binding do .NET converte números com a
cultura do sistema, e a máquina está em pt-BR, onde o ponto é separador de milhar — então o
`620.00` que o formulário mandava virava 62000. Corrigido recebendo o valor como texto e
convertendo com `CultureInfo.InvariantCulture` explícita, o que independe do idioma da máquina onde
a loja rodar.

### Segundo bug: acentos corrompidos na tela Ver
A view saiu com "ORDEM DE SERVIÃ‡O" por um erro de codificação na geração do arquivo. Regerada a
partir do original em UTF-8.

### Resultado
Verificado numa instalação limpa: duas OS salvas com numeração sequencial, valores corretos no
banco (R$ 620,00 e R$ 389,90), linha em branco do formulário ignorada, autoria gravada, transições
de situação com `DataEntrega` preenchida só na entrega, salvar sem cliente recusado, e exclusão de
cliente com histórico bloqueada com mensagem. O PDF de impressão sai em **uma folha A4** com as
duas vias, o número real e o nome do técnico na assinatura.

---

## [2026-08-07] Botões padronizados em 8px e cores unificadas

### Problema
Os botões do sistema misturavam dois formatos: **32 em `rounded-full`** (pílula) e 21 em
`rounded-lg` (8px). E havia três variações de cor para a mesma função.

### Arquivos Alterados

| Alteração | Alcance |
|---|---|
| `rounded-full` → `rounded-lg` (8px) em botões | 32 botões, 13 arquivos |
| `border-secondary text-secondary` → `border-outline text-on-surface-variant` | 3 botões (Orçamento e Estoque/Add) — havia 16 usando o cinza contra 3 no verde |
| `bg-secondary text-on-secondary` → `bg-secondary text-white` | 1 botão — mesma cor (`#ffffff`), token diferente |
| `bg-primary` → `bg-secondary` | 2 botões — "Confirmar" (Estoque) e "Adicionar" (Caixa) usavam o verde escuro `#003527` |
| `site.css`: verde e raio dos botões | "Adicionar à Lista" e "Gerar PDF" da Lista de Compras usavam `--pine #123f31` e raio de 6px |
| Ícone de voltar removido | `Views/Cliente/Add.cshtml` |

Ficou: **53 botões em 8px**, com apenas duas variantes de cor — **24 preenchidos**
(`bg-secondary #006c49` + `text-white`) e **18 de contorno** (`border-outline` +
`text-on-surface-variant`).

### O que ficou de fora, de propósito
- **Cards** mantidos em `rounded-xl`, conforme pedido.
- **Campos de busca** continuam `rounded-full` — são inputs, não botões.
- **Abas** (`np-step`, Estoque/Add) e **linhas de lista** (`painel-acao`, Estoque/Index) não têm
  raio: são sublinhado de aba e item de lista, não botões.
O verde escuro `bg-primary` deixou de ser usado em botões — segue apenas em títulos, na sidebar e
no painel das telas de entrada.

### Os dois sistemas visuais, agora com o mesmo botão
A Lista de Compras usa o `site.css` antigo, com paleta própria (`--pine #123f31`), e ficava de fora
da varredura do Tailwind. Foram criadas as variáveis `--acao: #006c49` e `--acao-ink: #005236`
usadas por `.btn-app-primary` e `.fab-pdf`, e o raio de `.btn-app` e `.acao-btn` subiu de 6px para
8px. O `--pine` continua valendo para navbar, links e títulos — só os botões foram unificados.

Isso adianta parte do item 15 do [ROADMAP](ROADMAP.md) (unificar os dois visuais): os botões das
duas famílias de tela agora são idênticos em cor e raio.

---

## [2026-08-07] Cadastro de clientes e integração com o Orçamento

### Problema
O formulário de Orçamento pedia nome, telefone, CPF/RG, CEP, endereço, número, bairro, cidade e UF
— **tudo redigitado a cada visita**. O cliente que voltava pela terceira vez era redigitado pela
terceira vez, com risco de divergência entre uma OS e outra. E não havia como responder "o que já
fizemos para esse cliente?".

É o item 1 do [ROADMAP](ROADMAP.md), e a metade que precisa vir primeiro: com o cadastro pronto, o
Orçamento passa a **referenciar** o cliente em vez de embutir os dados — que era o retrabalho que o
roadmap alertava para evitar.

### Solução
Módulo `Cliente` novo, seguindo o padrão de tela do Estoque: listagem com busca, cadastro, edição e
exclusão. Sem cartões de contagem — a lista é o conteúdo.

No Orçamento, os campos do cliente **deixaram de ser digitáveis**. O campo Nome ganhou uma lupa que
abre a busca; escolhido o cliente, os outros oito campos são preenchidos e todos ficam
`readonly`, o que garante que o que sai impresso na OS é igual ao que está no cadastro. Um botão de
fechar troca de cliente. O restante da tela não foi alterado.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Models/Cliente.cs` | **novo** — inclui `EnderecoCompleto`, o endereço em uma linha como sai na OS |
| `Controllers/ClienteController.cs` | **novo** — Index, Add/editar, Salvar, Excluir e `Buscar` (JSON, usado pelo Orçamento) |
| `Views/Cliente/Index.cshtml`, `Add.cshtml` | **novas** telas |
| `wwwroot/js/cliente.js` | **novo** — filtro da listagem no cliente |
| `Views/Orcamento/Index.cshtml` | campos do cliente `readonly`, lupa no Nome, modal de busca |
| `wwwroot/js/orcamento.js` | busca, seleção e limpeza do cliente |
| `Views/Home/Index.cshtml` | item "Clientes" na sidebar, abaixo de Dashboards |
| `Data/AppDbContext.cs` | `DbSet<Cliente>`, índice em Nome, `EnderecoCompleto` ignorado |
| Migration `AddClientes` | tabela nova |

### Bug encontrado e corrigido durante os testes
A busca por nome não achava nada: `maria` não encontrava "Maria Aparecida Souza". O EF Core traduz
`string.Contains` para `instr()` do SQLite, que **diferencia maiúsculas** — telefone e CPF
funcionavam só porque são números. Trocado por `EF.Functions.Like`, cujo `LIKE` do SQLite dobra
maiúsculas ASCII antes de comparar.

> Fica um limite conhecido: acentos não são normalizados, então `jose` não encontra "José".

### Resultado
Testado numa instalação limpa: 3 clientes cadastrados e gravados, busca por nome em qualquer caixa,
por telefone parcial e por CPF parcial, todas retornando o registro certo. O endpoint `Buscar`
exige login (redireciona anônimo). JS validado com `node --check` e os 34 ids que o
`orcamento.js` procura conferidos contra a view.

---

## [2026-08-07] ROADMAP reordenado a partir da comparação com o MapOS

### Motivo
Levantamento dos 19 módulos do [MapOS](https://github.com/RamonSilva20/mapos) (via API do GitHub)
comparado com o que o Pingo OS tem hoje. A conclusão que mudou a ordem: **os itens mais valiosos não
são módulos novos, são laços que nós mesmos abrimos e não fechamos** — imprimimos 90 dias de
garantia sem guardar nada, coletamos dados de cliente sem armazenar, temos linha de assinatura do
técnico sem registrar quem foi.

### Lacunas encontradas
Sem equivalente no Pingo OS: **Clientes**, Servicos, Garantias, Financeiro, Relatorios, Arquivos
(anexos), Auditoria, Permissoes, Cobrancas, Email. Também têm duas telas que valem copiar:
`imprimirOsTermica` (comprovante em impressora térmica de balcão) e `rel_receitas_brutas_mei`
(acompanhamento do teto de faturamento do MEI).

### Mudança de ordem
O ROADMAP listava "persistir Orçamento, Estoque e Caixa" como item único. Passou a começar por
**Clientes + Orçamento juntos**, com a dependência explícita no topo do arquivo: modelar a Ordem de
Serviço com os dados do cliente embutidos e criar o cadastro depois obrigaria a migrar dados na
marra.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `ROADMAP.md` | reescrito: 17 itens numerados por dependência, seção "A ordem importa" no topo, e o que foi recusado do MapOS (permissões granulares, cobranças) com o motivo |

---

## [2026-08-07] Licença Apache 2.0 e atribuição de terceiros

### Problema
O repositório era público **sem licença**, o que juridicamente equivale a "todos os direitos
reservados": ninguém podia legalmente usar, copiar ou modificar o código, mesmo estando visível.
A ilustração da tela de login, de terceiro, também estava sem o crédito que seu uso exige.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `LICENSE` | Apache 2.0, adicionada pelo GitHub. Corrigidos os colchetes do template na linha de copyright (`Copyright [2026] [Lucas Barros Mota]` → `Copyright 2026 Lucas Barros Mota`) — o próprio texto da licença instrui a removê-los |
| `NOTICE` | **novo** — materiais de terceiros: ilustração Storyset, Tailwind, Hanken Grotesk e Material Symbols, com as respectivas licenças |
| `README.md` | seção "Licença" com o resumo do que a Apache 2.0 permite e o aviso de que hospedar é por conta e risco de quem hospeda |
| `ROADMAP.md` | atribuição da ilustração deixa de ser pendência; sobra a troca por arte própria |

### Nota
A Apache 2.0 cobre o código do projeto, não os materiais de terceiros — daí o `NOTICE`, que é a
convenção da própria licença para declarar essas atribuições.

---

## [2026-08-07] Telas de entrada em duas colunas e alinhamento da tela de Usuários

### Problema
Login e recuperação de senha eram cards estreitos centralizados numa tela vazia — não pareciam
parte do ERP. A tela de Usuários usava `max-w-[1000px]`, enquanto todas as outras usam
`max-w-[1440px]`: o conteúdo ficava encolhido no meio, quebrando o alinhamento do sistema.

### Solução
Login e recuperação passaram a dividir a tela: **card à esquerda, painel de ilustração à direita**
(`grid lg:grid-cols-[1fr_1.05fr]`), com o painel extraído para o partial compartilhado
`_PainelIlustracao.cshtml` — as duas telas são a mesma moldura, muda só o card. Abaixo de `lg` o
painel some e o card ocupa a largura toda.

No login, o título do card é o próprio nome do sistema — **Pingo OS** — em vez de "Entrar": a tela
é a porta de entrada do produto, e o botão já diz qual é a ação. A recuperação de senha mantém o
título da ação, porque ali o usuário precisa saber em que página está. O painel à direita ficou só
com a ilustração, centralizada e alinhada com o card, sobre o verde institucional (`primary #003527`).

O bloco de identidade da loja saiu do card: exibia o nome da loja onde se espera o nome do sistema,
e a loja já aparece na barra superior depois do login.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Views/Shared/_PainelIlustracao.cshtml` | **novo** — painel direito compartilhado |
| `Views/Conta/Login.cshtml` | reescrita em duas colunas; só o formulário no card |
| `Views/Conta/EsqueciSenha.cshtml` | reescrita na mesma moldura do login |
| `Views/Conta/Usuarios.cshtml` | `max-w-[1000px]` → `max-w-[1440px]` |
| `wwwroot/img/code-typing.png` | **novo** — ilustração Storyset recolorida |
| `docs/screenshots/` | login, recuperar e usuarios atualizados |

### Sobre a ilustração
O Storyset não serve o SVG do estilo *cuate* publicamente — só o PNG 600×400, no amarelo padrão
(`#FFC727`). Baixei o PNG e remapeei a família do amarelo para o verde `#357A49` pedido,
preservando as variações de tom (10.566 pixels). Exibida em no máximo 600px — a resolução nativa
do arquivo, o limite antes de perder nitidez.

> **Atribuição:** ilustração da [Storyset](https://storyset.com) (Freepik). O uso gratuito exige
> crédito visível no projeto — ainda **não** adicionado. Ver ROADMAP.

---

## [2026-08-07] Recuperação de senha: código de recuperação e comando de terminal

### Problema
Com o login recém-criado, o administrador que esquecesse a própria senha ficava **trancado fora do
sistema**. Não havia "esqueci minha senha" (o fluxo comum depende de e-mail, que o sistema não
envia) e só um outro administrador poderia redefinir — inútil numa loja com um único dono.
O caso deixou de ser hipotético: aconteceu de fato nesta data, e a única saída foi forjar o hash
PBKDF2 na mão e escrever direto no `loja.db`.

### Solução
Duas camadas, para que não exista cenário de tranca:

1. **Código de recuperação** (`XXXX-XXXX-XXXX-XXXX`, ~79 bits, alfabeto sem `0/O` e `1/I/L` para
   não errar ao copiar do papel). Gerado na criação de cada conta e mostrado **uma única vez**;
   o banco guarda apenas o hash, com o mesmo `PasswordHasher` da senha. Com ele a pessoa redefine
   a própria senha na tela de login, sem depender de ninguém. O código usado é queimado e outro é
   entregue na hora.
2. **Comando de terminal**, para quando o código também se perder. Faz sentido porque o sistema
   roda local: quem alcança a máquina já alcança o `loja.db`.
   ```bash
   dotnet run -- redefinir-senha dono@loja.com novasenha123
   ```

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Models/Usuario.cs` | campo `CodigoRecuperacaoHash` |
| `Data/CodigoRecuperacao.cs` | **novo** — geração e normalização do código |
| `Controllers/ContaController.cs` | `EsqueciSenha`, `CodigoDeRecuperacao`, `GerarCodigo` |
| `Views/Conta/EsqueciSenha.cshtml`, `CodigoDeRecuperacao.cshtml` | **novas** telas |
| `Views/Conta/Login.cshtml`, `Usuarios.cshtml` | link "Esqueci minha senha" e botão "Novo código" |
| `Program.cs` | comando `redefinir-senha` antes de subir o web host |
| Migration `AddCodigoRecuperacao` | coluna nova |

### Bug encontrado e corrigido durante os testes
A tela que exibe o código exigia login. Quem acabava de recuperar a senha ainda **não estava
logado**, era desviado para o login e **nunca via o código novo** — ficando sem código para a
próxima vez, exatamente o problema que a funcionalidade veio resolver. Corrigido com
`[AllowAnonymous]` na ação: ela só mostra o que está no `TempData` da própria sessão.

### Resultado
Ciclo completo testado numa instalação limpa (publicada em pasta separada, sem tocar no banco de
desenvolvimento): instalação → código entregue → recuperação com o código digitado sem hífen e em
minúscula → código novo entregue → senha nova entra e a antiga não. Recusas confirmadas **pelo
efeito** (a senha não muda) para código inventado, código já queimado e e-mail inexistente. O
comando de terminal valida argumentos, aceita e-mail em qualquer caixa, reativa conta desativada,
e a senha que ele define funciona no site.

---

## [2026-08-07] Autenticação: login, primeiro acesso e usuários

### Problema
O sistema não tinha login. A barra superior exibia o rótulo "Administrador", mas qualquer pessoa
que abrisse a URL via os dados dos clientes — incluindo CPF, endereço e telefone coletados na
Ordem de Serviço.

### Solução
Login por cookie com o `PasswordHasher` do ASP.NET Core (PBKDF2-SHA512, 100 mil iterações, salt de
16 bytes), **sem** arrastar o pacote Identity inteiro e suas tabelas. Todas as rotas exigem login
por padrão via filtro global; o que é público leva `[AllowAnonymous]`.

Sem nenhum usuário cadastrado, o sistema só abre a tela de **primeiro acesso**, que cria a conta do
dono como `Admin` e já nomeia a loja — resolvendo de quebra o "Minha Loja" genérico. Depois disso a
tela se fecha e o cadastro de novos usuários fica restrito a administradores.

Dois papéis: **Admin** (gerencia usuários) e **Técnico** (usa o sistema). Usuários são
**desativados, nunca excluídos**, para que o histórico de quem emitiu cada OS continue fazendo
sentido; e o admin não consegue desativar a própria conta.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Models/Usuario.cs` | **novo** — entidade e constantes de papéis |
| `Controllers/ContaController.cs` | **novo** — login, logout, primeiro acesso, gestão de usuários |
| `Views/Conta/` | **novas** — Login, PrimeiroAcesso, Usuarios, NovoUsuario, SemPermissao |
| `Views/Shared/_HeadTailwind.cshtml` | **novo** — `<head>` comum das telas Tailwind (evitava copiar 51 linhas de config em 5 telas) |
| `Views/Shared/_Navbar.cshtml` | mostra o usuário logado, "Sair" e "Usuários" (só admin) |
| `Program.cs` | autenticação por cookie, filtro global, redirecionamento de primeiro acesso |
| `Data/AppDbContext.cs` | `DbSet<Usuario>` e índice único no e-mail |
| `Controllers/HomeController.cs` | `[AllowAnonymous]` na página de erro |
| Migration `AddUsuarios` | tabela nova |

### Resultado
19 cenários verificados com a aplicação no ar: redirecionamento de primeiro acesso, fechamento da
tela após instalado, `ReturnUrl` preservado, mensagem única para senha errada e e-mail inexistente
(não revela quais e-mails existem), e-mail normalizado para minúsculas, duplicado rejeitado,
**técnico usa o sistema mas é barrado em Usuários**, usuário desativado não entra, admin não se
autodesativa.

---

## [2026-08-07] Sessão reduzida de 7 dias para 8 horas

### Problema
Após criar a conta, o sistema abria direto no painel em vez da tela de login. A proteção estava
correta — era a sessão ainda válida. Mas ela durava 7 dias e navegadores baseados em Chromium
restauram cookies de sessão ao reabrir, então a tela de login praticamente nunca reaparecia.

### Arquivos Alterados

| Arquivo | Antes | Depois |
|---|---|---|
| `Program.cs` | `ExpireTimeSpan` de 7 dias | 8 horas, com `SlidingExpiration` |
| `Program.cs` | `new AuthorizeFilter()` | `new AuthorizeFilter(policy)` com `RequireAuthenticatedUser()` explícito |

### Resultado
Renova enquanto o sistema está em uso e expira da noite para o dia: o balcão começa o expediente
pedindo login. A troca do filtro não muda comportamento — a forma sem política depende de um padrão
implícito, e em algo que protege dado de cliente é melhor estar escrito.

---

## [2026-08-07] README reformulado e ROADMAP criado

### Problema
O `README.md` era um guia interno de desenvolvimento: não dizia o que o sistema faz, não tinha
imagem, instalação, requisitos nem stack. E estava **desatualizado em cinco pontos**.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `README.md` | reescrito: badges, screenshots, índice, requisitos, instalação, stack, estrutura — mantendo os guias de módulos e de banco |
| `docs/screenshots/` | **novo** — 6 capturas reais (Painel, Orçamento, Estoque, Lista de compras, Login, Primeiro acesso) |
| `ROADMAP.md` | **novo** — melhorias futuras por prioridade, com o *porquê* de cada uma e o que foi descartado |

### Correções de conteúdo desatualizado

| Dizia | Realidade |
|---|---|
| Caixa, Estoque e Orçamento "🚧 Em breve" | os três já têm telas; só Dashboards é placeholder |
| "Sem barra lateral, largura cheia" | o Painel tem sidebar fixa desde o commit `84281a4` |
| KPIs são "dados de exemplo" com chip | o chip não existe mais; mostram estado vazio |
| Skill `frontend-design` em `.agents/skills/` | removida do repositório |
| Navbar em `_Layout.cshtml` | é o partial `_Navbar`; o `_Layout` só serve à página de erro |

Também trocou o status binário (pronto/em breve) por três níveis, porque a distinção que importa
hoje é **tem tela mas não grava no banco**.

---

## [2026-08-06] Ordem de Serviço em duas vias com termos legais

### Problema
A impressão do orçamento saía com uma via só, sem espaço para cliente e técnico assinarem cada um
a sua, e sem nenhuma condição de serviço registrada no papel.

### Solução
O documento passou a sair com **duas vias idênticas na mesma folha A4** (1ª via do cliente, 2ª do
técnico), separadas por linha de corte. A segunda é clonada por JavaScript a partir da primeira, o
que garante que qualquer campo novo apareça nas duas automaticamente.

Cada via traz um bloco de **termos e condições** em 9 cláusulas com as bases legais: aprovação
prévia e irretratabilidade do orçamento (CDC art. 40 §2º), backup por conta do cliente, **sigilo
sobre os arquivos pessoais do aparelho** (LGPD 13.709/2018 e CP art. 154-A), garantia de 90 dias
(CDC art. 26, II), exclusão de mau uso (CDC art. 14 §3º, II), perda de garantia por violação de
terceiros, riscos de falhas ocultas, **prazo de retirada de 90 dias e abandono** (CC art. 1.275,
III) e identificação na entrega.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Views/Orcamento/Index.cshtml` | estrutura de vias, linha de corte, selo de via, bloco de termos, CSS de impressão compactado |
| `wwwroot/js/orcamento.js` | `gerarSegundaVia()` clona a 1ª via; `ajustarEscala()` encolhe o documento se passar da folha |

### Medição
Com 4 itens o documento dava 1116px contra 1062px de área útil da A4, estourando para a segunda
página. Daí a compactação (fonte base 10px, margem 8mm, Cliente em 3 colunas, Aparelho em 4) mais
o ajuste automático de escala, que no teste ficou em 0,951 — 5% de redução, imperceptível.
Verificado gerando o PDF: **uma folha**. Acima de ~13 itens o piso de escala trava e vira 2 páginas,
o que é inevitável.

### Ressalva registrada
Os termos ficam em ~5,3pt. O **CDC art. 54 §3º** exige fonte "não inferior ao corpo doze" em
contratos de adesão. Em corpo 12 o texto ocuparia ~440px por via e não haveria como pôr duas vias
na folha. A decisão foi manter pequeno (a maioria dos clientes tem impressora simples e a
alternativa exigia frente e verso), ciente de que as cláusulas restritivas — exclusões, violação e
abandono — são as mais expostas a questionamento. **O texto não é parecer jurídico e merece
revisão de um advogado**, em especial a cláusula de abandono: o art. 1.275, III do CC trata da
perda da propriedade, mas a jurisprudência costuma exigir notificação comprovada e não valida a
venda automática do bem.

---

## [2026-08-06] NU1903 resolvido — pin do SQLitePCLRaw fixava a versão vulnerável

### Problema
O `.csproj` já tinha um `PackageReference` direto para `SQLitePCLRaw.lib.e_sqlite3`, adicionado seguindo a "ação recomendada" registrada aqui ("fixar versão segura"). Só que ele fixava a **v2.1.11 — a própria versão vulnerável**. O pin nunca teve efeito e o aviso NU1903 seguia em todo build.

### Causa Raiz
A v2.1.11 embute o SQLite 3.49.1. O CVE-2025-6965 (CVSS 7.2) exige SQLite ≥ 3.50.2. A página do GitHub Advisory ainda informa "no patched version available", mas a **v2.1.12** (jul/2026) já embute o SQLite 3.53.3 e corrige o problema, mantendo a linha 2.1.x compatível com o resto da stack que o EF Core 10 puxa.

### Arquivos Alterados

| Arquivo | Antes | Depois |
|---|---|---|
| `ListasCompras.csproj` | `SQLitePCLRaw.lib.e_sqlite3` v2.1.11 | v2.1.12 |

### Resultado
- Build com **0 erros e 0 avisos** (antes: 2 avisos NU1903).
- `libe_sqlite3.so` publicado passa de SQLite 3.49.1 para **3.53.3**, verificado no binário.
- O `PackageReference` direto **deve continuar existindo**: é ele que sobrescreve a v2.1.11 que o `Microsoft.EntityFrameworkCore.Sqlite` traz por transitividade.

---

## [2026-08-06] Remoção de código e dependências não utilizados

### Problema
Sobras de versões anteriores e do template padrão do ASP.NET ocupavam o repositório: bibliotecas front-end nunca carregadas, JS órfãos, seções de CSS de telas já reescritas em Tailwind e uma entidade EF nunca usada.

### Arquivos Alterados

| Removido | Motivo |
|---|---|
| `wwwroot/lib/bootstrap`, `jquery`, `jquery-validation`, `jquery-validation-unobtrusive` (9,5 MB) | Sem nenhuma referência; nada no código usa `$`/`jQuery` |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | Nunca renderizado — todas as views usam `Layout = null`, exceto `Error.cshtml` |
| `wwwroot/js/site.js` | Só comentários do template padrão |
| `wwwroot/js/configuracao.js` | Órfão: não referenciado e 3 dos 5 IDs que busca não existem mais na tela |
| `Innovation-amico.svg`, `config.gif` | Sem referência; o `.gif` nem estava em `wwwroot` |
| `site.css` (207 linhas), `print.css` (30 linhas) | Seções `Configurações`, `Painel de módulos` e `.report-topbar` — telas reescritas em Tailwind |
| Pacote `QuestPDF` | Nenhum `using`; o PDF sai da impressão do navegador |
| `Models/ProdutoModeloCompatibilidade.cs` + `DbSet` + navegações `Compatibilidades` | Nunca lida nem gravada; tabela vazia. Migration `RemoveProdutoModeloCompatibilidade` dropa a tabela (`Down` recria) |
| `using ListasCompras.Models` em `OrcamentoController.cs` | Controller só faz `return View()` |

### Resultado
- 65 arquivos e ~82.700 linhas a menos; `wwwroot/` cai de ~9,6 MB para 88 KB.
- Build limpo e as 7 rotas verificadas com a aplicação no ar, sem assets quebrados nem exceções.
- Dados preservados (7 categorias, 4 marcas, 30 modelos, 12 produtos).

---

## [2026-07-01] Namespace incorreto em SeedData.cs (recorrência)

### Problema
Ao adicionar `Data/SeedData.cs` (populador inicial de categorias/marcas/modelos) e integrá-lo ao `Program.cs`, o novo arquivo repetiu o mesmo erro já corrigido anteriormente: foi declarado com o namespace singular `ListaCompras` (sem 's') em vez de `ListasCompras`, e `Program.cs` importava `using ListaCompras.Data;` além do `using ListasCompras.Data;` correto — o projeto não compilava (`CS0246`).

### Arquivos Alterados

| Arquivo | Antes | Depois |
|---|---|---|
| `Data/SeedData.cs` | `namespace ListaCompras.Data` + `using ListaCompras.Models` | `namespace ListasCompras.Data` + `using ListasCompras.Models` |
| `Program.cs` | `using ListasCompras.Data;` + `using ListaCompras.Data;` (duplicado/errado) | apenas `using ListasCompras.Data;` |

### Resultado
- Build volta a compilar sem erros (`dotnet build` — 0 erros, 2 avisos, mesmo aviso pendente de `SQLitePCLRaw.lib.e_sqlite3` já registrado abaixo).
- `Program.cs` agora chama `SeedData.Initialize(db)` após `db.Database.Migrate()`, populando o banco com categorias, marcas e modelos de celular na primeira execução.

---

## [2026-07-01] Correção de Namespaces Inconsistentes

### Problema
O projeto usa o nome `ListasCompras` (com 's'), mas 8 arquivos declaravam namespace como `ListaCompras` (sem 's'), causando 3 erros de compilação (`CS0246 - tipo ou namespace não encontrado`):

```
Models/Categoria.cs(7): error CS0246 – 'Produto' não encontrado
Models/Produto.cs(10): error CS0246 – 'Categoria' não encontrado
Data/AppDbContext.cs(10): error CS0246 – 'Categoria' não encontrado
```

### Causa Raiz
Mistura entre dois namespaces distintos:
- **Correto** (`ListasCompras.*`): `Categoria.cs`, `ErrorViewModel.cs`, `HomeController.cs`
- **Incorreto** (`ListaCompra.*`): todos os demais arquivos

### Arquivos Alterados

| Arquivo | Antes | Depois |
|---|---|---|
| `Models/Produto.cs` | `namespace ListaCompras.Models` | `namespace ListasCompras.Models` |
| `Models/ListaCompra.cs` | `namespace ListaCompras.Models` | `namespace ListasCompras.Models` |
| `Models/ItemListaCompra.cs` | `namespace ListaCompras.Models` | `namespace ListasCompras.Models` |
| `Models/MarcaCelular.cs` | `namespace ListaCompras.Models` | `namespace ListasCompras.Models` |
| `Models/ModeloCelular.cs` | `namespace ListaCompras.Models` | `namespace ListasCompras.Models` |
| `Models/ProdutoModeloCompatibilidade.cs` | `namespace ListaCompras.Models` | `namespace ListasCompras.Models` |
| `Data/AppDbContext.cs` | `namespace ListaCompras.Data` + `using ListaCompras.Models` | `namespace ListasCompras.Data` + `using ListasCompras.Models` |
| `Program.cs` | `using ListaCompras.Data` | `using ListasCompras.Data` |

### Resultado
- **Antes:** 3 erros, 2 avisos  
- **Depois:** 0 erros, 2 avisos (aviso de vulnerabilidade em `SQLitePCLRaw.lib.e_sqlite3` v2.1.11 — pendente de atualização de pacote)

---

## Avisos Pendentes

Nenhum. O NU1903 foi resolvido em 2026-08-06 (ver a primeira entrada acima).
