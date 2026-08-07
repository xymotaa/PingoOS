# Melhorias futuras

O que ainda não existe no Pingo OS, por que faz falta e o que envolve fazer. Não é uma promessa
de entrega nem tem prazo — é a lista do que já foi discutido e decidido adiar, para não se perder.

Quando um item for feito, tire daqui e registre no [CHANGES.md](CHANGES.md).

> Boa parte desta lista saiu da comparação com o [MapOS](https://github.com/RamonSilva20/mapos),
> que resolve o mesmo problema (assistência técnica) há mais tempo. O que copiamos e o que
> recusamos está marcado item a item.

---

## A ordem importa

Três itens têm dependência entre si e **precisam ser feitos na sequência abaixo**, senão geram
retrabalho garantido:

```
✅ Clientes                          (feito em 2026-08-07)
✅ Ordem de Serviço no banco         (feito em 2026-08-07, já com autoria)
        ↓
1. Estoque e Caixa no banco
        ↓
2. Garantia e anexos                 (penduram na OS, que já está gravada)
        ↓
3. Financeiro e relatórios           (precisam de venda gravada)
```

O erro que essa ordem evita: modelar a Ordem de Serviço com os dados do cliente embutidos e criar o
cadastro de Clientes depois — o que obrigaria a migrar dados na marra. Por isso Clientes veio antes.

---

## Alta prioridade

### 1. Estoque e Caixa no banco
**Falta porque** as telas existem mas não gravam. O Estoque usa `localStorage` (chave
`xyEstoqueProdutos`) como ponte temporária: os dados somem se o usuário limpar o navegador e não
existem em outra máquina. O Caixa monta a venda na tela e a perde ao sair.

**O que envolve:** modelar produto de estoque (saldo, mínimo, custo, preço) e a venda do Caixa com
seus itens, e trocar o `localStorage` por consultas ao servidor — o `estoque.js` tem 517 linhas
construídas em torno dele. No MapOS são os módulos `Produtos` e `Vendas`.

### 2. Garantias
**Falta porque** nós **imprimimos** 90 dias de garantia em toda Ordem de Serviço e não guardamos
nada. O cliente volta dizendo "está na garantia" e não há como conferir a data, o que foi trocado,
nem se já houve retorno pelo mesmo defeito. É um laço que abrimos e não fechamos.

**O que envolve:** data de início (a retirada), prazo, vínculo com a OS e com as peças trocadas, e
uma tela de consulta rápida por aparelho ou cliente. Barato depois que a OS estiver no banco.

### 3. Backup do banco pela tela de Configuração
**Falta porque** hoje o backup depende de a pessoa saber copiar `loja.db` pelo terminal — ou seja,
na prática não acontece. Um dono de loja não vai fazer isso, e o banco guarda cadastro de clientes,
ordens de serviço e histórico.

**O que envolve:** um botão que devolve o `loja.db` como download, e outro que restaura a partir de
um arquivo enviado. A parte delicada é a restauração: precisa fechar as conexões do EF Core antes
de sobrescrever o arquivo, ou o SQLite recusa.

---

## Média prioridade

### 4. Catálogo de serviços com preço
**Falta porque** os itens do orçamento são texto livre. Cada técnico escreve de um jeito e cobra o
que lembra. Um catálogo ("troca de tela", "limpeza de placa") dá preço consistente e preenchimento
rápido. É o módulo `Servicos` do MapOS.

**O que envolve:** entidade simples (nome, valor padrão) e um seletor no formulário do Orçamento,
mantendo a possibilidade de digitar item livre.

### 5. Anexar fotos do aparelho
**Falta porque** é proteção jurídica direta: fotografar o aparelho na entrada é a defesa contra
"esse arranhão não estava aí". Reforça exatamente as cláusulas de risco que já estão impressas nos
termos da OS. O MapOS chama de `Arquivos`.

**O que envolve:** upload vinculado à OS. Decidir onde guardar — arquivo em pasta é melhor que
base64 no banco, que é como a logo da loja é armazenada hoje e não escalaria para fotos.

### 6. Relatório de receita bruta MEI
**Falta porque** MEI tem teto anual de faturamento, e quem passa sem perceber é desenquadrado e cai
numa carga tributária maior. O MapOS tem um relatório dedicado a isso
(`rel_receitas_brutas_mei`) — é o item mais específico do Brasil na lista deles e vale mais para o
dono da loja do que qualquer gráfico bonito.

**O que envolve:** somar as vendas e ordens do ano contra o teto vigente, com aviso ao se aproximar.
Depende de Caixa e OS gravando.

### 7. Impressão térmica da OS
**Falta porque** o balcão usa impressora térmica de 58/80mm no dia a dia. Nosso documento A4 de duas
vias está bem resolvido para a assinatura, mas o comprovante de entrada entregue na hora é térmico.
O MapOS mantém os dois (`imprimirOs` e `imprimirOsTermica`).

**O que envolve:** uma segunda folha de estilo de impressão, em coluna estreita, sem tabela larga.
Convivem: térmica na entrada, A4 na assinatura.

### 8. Notificação ao cliente (e-mail ou WhatsApp)
**Falta porque** os termos da OS dizem que, para considerar um aparelho abandonado, o cliente
precisa ter sido **notificado por escrito**. Hoje o sistema não produz essa prova. Isso deixa de ser
conveniência e passa a ser o que sustenta juridicamente a cláusula 8 — além de resolver o "seu
aparelho está pronto".

**O que envolve:** configuração de SMTP, uma tabela de notificações enviadas (data e destinatário,
que é a prova) e um `BackgroundService` para reenviar as que falharem.

> Nota de stack: **não precisamos de cron.** O `BackgroundService` do .NET roda dentro da própria
> aplicação. O MapOS precisa de duas linhas no crontab do servidor para a mesma coisa.

### 9. Financeiro (lançamentos e contas a pagar)
**Falta porque** Caixa sem persistência não é caixa. Entradas, saídas e contas a pagar dão a visão
do mês, que hoje não existe em lugar nenhum. É o módulo `Financeiro` do MapOS.

**O que envolve:** maior esforço da lista; depende de Caixa e OS gravando.

### 10. Script de instalação para o usuário final
**Falta porque** o público do sistema é dono de loja, não desenvolvedor. Hoje instalar exige clonar
o repositório, ter o SDK do .NET e rodar comandos. Essa é a lição que o MapOS acerta: sem instalação
simples, o sistema não chega em quem precisa dele.

**O que envolve:** um `install.sh` (Linux) e um `install.bat` (Windows) que instalam o runtime,
publicam, registram como serviço do sistema (systemd / Serviço do Windows) e abrem o navegador. Bem
mais simples que no MapOS: não há PHP, MySQL nem webserver para configurar.

---

## Baixa prioridade

### 11. Recuperação de senha por e-mail
**Já existe** recuperação por código anotado no papel e por comando de terminal (2026-08-07, ver
[CHANGES.md](CHANGES.md)). Falta a via por e-mail, que dispensa guardar código: link de uso único
com validade curta.

**Depende de** o envio de e-mail (item 8). Prioridade baixa agora que o caso de tranca está coberto.

### 12. Ilustração própria para a tela de login
**Situação atual:** a atribuição da [Storyset](https://storyset.com) está no arquivo `NOTICE`, então
a obrigação de crédito está cumprida.

**O que ainda incomoda:** "code typing" mostra alguém programando, sem relação com assistência
técnica de celular. Uma ilustração do mundo da loja — bancada, aparelho aberto, ferramenta — diria
mais e dispensaria a dependência de terceiro.

### 13. Unificar os dois visuais
**Situação atual:** convivem dois sistemas de design. As telas novas (Painel, Orçamento, Estoque,
Caixa, Configuração, Login) usam Tailwind com paleta Material-3. As antigas (Lista de compras,
"Em breve") usam `wwwroot/css/site.css`, verde institucional com fonte Inter.

**O que envolve:** migrar `Views/ListaCompra/` e `Views/Shared/EmBreve.cshtml` para Tailwind,
aproveitando o partial `_HeadTailwind.cshtml`. Depois o `site.css` encolhe bastante. Cosmético.

### 14. Dashboards de verdade
**Situação atual:** `DashboardsController` retorna a tela "Em breve" e os KPIs do Painel mostram
estado vazio.

**O que envolve:** depende de Caixa, Estoque e OS no banco — sem dado gravado não há o que somar.

### 15. Autoria detalhada (auditoria mínima)
**Situação atual:** o item 4 resolve o essencial (quem emitiu a OS). Uma auditoria completa, como o
módulo `Auditoria` do MapOS, registraria toda alteração em todo registro.

**O que envolve:** só vale se a loja tiver vários técnicos e surgir a pergunta "quem alterou isso?".
Antes disso é peso sem retorno.

---

## Descartado (e por quê)

### Permissões granulares por tela
O MapOS tem um módulo inteiro (`Permissoes`) para configurar acesso tela a tela. **Não vamos
fazer.** Numa loja com dono e um ou dois técnicos, os dois papéis que já existem (Admin e Técnico)
resolvem. É complexidade de configuração que ninguém vai ajustar.

### Cobranças e boletos
O módulo `Cobrancas` do MapOS emite cobrança bancária. Exige integração com instituição financeira
e cadastro formal. Fora de escala para o projeto.

### Atualização automática pelo próprio sistema
O MapOS tem um botão "Atualizar Mapos" que baixa e substitui os arquivos. **Não vamos fazer.**
Em PHP funciona porque atualizar é sobrescrever arquivos de texto reinterpretados a cada
requisição. No .NET o processo em execução mantém as DLLs carregadas: seria preciso um processo
supervisor separado para parar o serviço, trocar os arquivos e subir de novo — com risco real de
deixar a loja com o sistema quebrado e ninguém por perto para consertar. O ganho não paga o risco.

Atualizar continua sendo `git pull` (ou baixar a nova versão) e rodar de novo; as migrations do
banco são aplicadas sozinhas na inicialização.

### Assistente de instalação nos moldes do MapOS
Metade do assistente deles serve para coletar host, usuário e senha do MySQL — que aqui não
existem, porque o SQLite é um arquivo. A outra metade (dados do responsável e da loja) já foi
resolvida pela tela de **primeiro acesso**.

### Docker
Faria sentido para hospedar em servidor atendendo várias lojas. **A decisão foi rodar só local**,
na máquina da própria loja, então Docker seria peso morto: `dotnet publish` já entrega uma pasta
com executável, sem daemon para instalar.

Se alguém quiser hospedar por conta própria, fica a cargo da pessoa — e nesse caso ela precisa
resolver HTTPS e revisar a exposição dos dados pessoais dos clientes (CPF, endereço, telefone) que
o sistema armazena.
