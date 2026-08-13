# Registro de Alterações

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
