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
✅ Clientes                          (2026-08-07)
✅ Ordem de Serviço no banco         (2026-08-07, já com autoria)
✅ Estoque e Caixa no banco          (2026-08-07, venda baixa o estoque)
✅ Garantias                         (2026-08-11, conta da data de entrega)
✅ Backup pela tela                  (2026-08-11)
✅ Catálogo de serviços              (2026-08-11, alimenta os itens da OS)
✅ Orçamento separado da OS          (2026-08-12, aprovado vira ordem)
✅ Fotos do aparelho                 (2026-08-13, evidência na entrada)
✅ Faturamento MEI                   (2026-08-13, soma contra o teto de R$ 81.000)
        ↓
1. Financeiro e relatórios           (a venda já está gravada; falta somar)
```

O erro que essa ordem evita: modelar a Ordem de Serviço com os dados do cliente embutidos e criar o
cadastro de Clientes depois — o que obrigaria a migrar dados na marra. Por isso Clientes veio antes.

---

## Alta prioridade

### 1. Impressão térmica da OS
**Falta porque** o balcão usa impressora térmica de 58/80mm no dia a dia. Nosso documento A4 de duas
vias está bem resolvido para a assinatura, mas o comprovante de entrada entregue na hora é térmico.
O MapOS mantém os dois (`imprimirOs` e `imprimirOsTermica`).

**O que envolve:** uma segunda folha de estilo de impressão, em coluna estreita, sem tabela larga.
Convivem: térmica na entrada, A4 na assinatura.

### 2. Notificação ao cliente (e-mail ou WhatsApp)
**Falta porque** os termos da OS dizem que, para considerar um aparelho abandonado, o cliente
precisa ter sido **notificado por escrito**. Hoje o sistema não produz essa prova. Isso deixa de ser
conveniência e passa a ser o que sustenta juridicamente a cláusula 8 — além de resolver o "seu
aparelho está pronto".

**O que envolve:** configuração de SMTP, uma tabela de notificações enviadas (data e destinatário,
que é a prova) e um `BackgroundService` para reenviar as que falharem.

> Nota de stack: **não precisamos de cron.** O `BackgroundService` do .NET roda dentro da própria
> aplicação. O MapOS precisa de duas linhas no crontab do servidor para a mesma coisa.

### 3. Financeiro (lançamentos e contas a pagar)
**Falta porque** Caixa sem persistência não é caixa. Entradas, saídas e contas a pagar dão a visão
do mês, que hoje não existe em lugar nenhum. É o módulo `Financeiro` do MapOS.

**O que envolve:** maior esforço da lista; depende de Caixa e OS gravando.

### 3.1. Fechamento de caixa do dia (discutido em 2026-08-14)
**Falta porque** muita loja bate o dinheiro físico da gaveta contra o sistema no fim do expediente,
e hoje Vendas e Ordens de Serviço entregues não têm um relatório diário — só o total anual da tela
de Faturamento MEI.

**O que envolve:** relatório do dia (não altera nada, só lê): Vendas por forma de pagamento
(dinheiro/débito/crédito/PIX), **separado** de Ordens de Serviço entregues por forma de pagamento,
com um total geral abaixo dos dois. A separação visual é proposital — o risco discutido foi o
usuário lançar a mesma OS como venda no Caixa "para aparecer no fechamento", duplicando o
faturamento que a tela de Faturamento MEI já soma. O relatório deve deixar claro que são duas
fontes diferentes que só estão lado a lado para conferência, nunca a mesma coisa.

**Depende de** nada além do que já é gravado hoje (`Venda.FormaPagamento`, `OrdemServico.FormaPagamento`
+ `Sinal`). Pode vir antes do item 3 (Financeiro) — é mais simples, é leitura pura.

### 3.2. Ordem de compra (peça sob encomenda) — ideia solta (2026-08-14)
**Falta porque** nem toda peça (ex: frontal de um modelo específico) está em estoque; a loja
encomenda de uma distribuidora e a peça chega depois. Hoje não há como registrar isso — só existe
lançar a peça no Estoque quando ela já chegou.

**O que envolve, em linhas gerais:** uma tela no molde da Ordem de Serviço, mas de compra: dados da
distribuidora/fornecedor, item(ns) pedido(s), situação (Pedida → Chegou → Cancelada). Ao chegar,
provavelmente alimenta o Estoque — parecido com o que o catálogo de Serviços faz para a OS. Ainda
não desenhado; entra no roadmap só para não perder a ideia. Repensar o relacionamento com
`ProdutoEstoque` e `MovimentacaoEstoque` quando for para frente.

### 4. Script de instalação para o usuário final
**Falta porque** o público do sistema é dono de loja, não desenvolvedor. Hoje instalar exige clonar
o repositório, ter o SDK do .NET e rodar comandos. Essa é a lição que o MapOS acerta: sem instalação
simples, o sistema não chega em quem precisa dele.

**O que envolve:** um `install.sh` (Linux) e um `install.bat` (Windows) que instalam o runtime,
publicam, registram como serviço do sistema (systemd / Serviço do Windows) e abrem o navegador. Bem
mais simples que no MapOS: não há PHP, MySQL nem webserver para configurar.

### 5. Hospedar na nuvem (Supabase + domínio próprio)
**Mudou de ideia (2026-08-12):** a decisão registrada em "Descartado" era rodar só local. O Supabase
tem um free tier de Postgres, o que reabre a questão — banco gerenciado sem custo muda a conta.

**O que envolve, em ordem:**

1. **Banco: SQLite → Postgres.** Trocar o provider do EF Core
   (`Npgsql.EntityFrameworkCore.PostgreSQL`) e **recriar as migrations do zero** — as atuais têm
   tipos e sintaxe específicos do SQLite (`INTEGER`, `TEXT`), não são portáveis por cima. Revisar:
   - `EF.Functions.Like` (busca case-insensitive) — no Postgres o equivalente correto é `ILIKE`.
   - Backup pela tela usa `VACUUM INTO`, exclusivo do SQLite. No Supabase, backup automático já vem
     incluso; a tela perderia a razão de existir como está, ou vira um export adicional (`pg_dump`).
   - Logo da loja em base64 no banco (`ConfiguracaoLoja`) funciona igual num Postgres.
2. **Login "real" em produção.** A autenticação já é real (cookie + PBKDF2-SHA512); o que falta é
   contexto de produção: cookie `Secure`, HTTPS obrigatório, e connection string/segredos saindo do
   `appsettings.json` para variável de ambiente.
3. **Hospedagem do binário e domínio.** Duas rotas, a decidir mais perto da hora:
   - **VPS da Hostinger** (não hospedagem compartilhada — essa não roda .NET) com o runtime
     instalado, o app como serviço `systemd` atrás de nginx fazendo HTTPS (Let's Encrypt).
   - **Docker**, reabrindo o item descartado abaixo: builda uma imagem com o `dotnet publish`, sobe
     num VPS com `docker run`, e o domínio da Hostinger aponta pra lá. Mais portável entre provedores
     se um dia trocar de VPS, ao custo de manter um `Dockerfile`.

**Consequência que muda a natureza do sistema:** dados pessoais de clientes (CPF, endereço,
telefone) passam a ficar num servidor exposto à internet, não mais só no PC da loja. É a mesma
ressalva que já estava na nota de Docker abaixo, e vale ainda mais aqui.

**Ordem recomendada:** migrar para Postgres primeiro (testável de graça, local ou já no Supabase),
validar tudo funcionando, e só depois resolver hospedagem — misturar as duas frentes dificulta saber
qual delas quebrou alguma coisa.

---

## Baixa prioridade

### 6. Recuperação de senha por e-mail
**Já existe** recuperação por código anotado no papel e por comando de terminal (2026-08-07, ver
[CHANGES.md](CHANGES.md)). Falta a via por e-mail, que dispensa guardar código: link de uso único
com validade curta.

**Depende de** o envio de e-mail (item 3). Prioridade baixa agora que o caso de tranca está coberto.

### 7. Ilustração própria para a tela de login
**Situação atual:** a atribuição da [Storyset](https://storyset.com) está no arquivo `NOTICE`, então
a obrigação de crédito está cumprida.

**O que ainda incomoda:** "code typing" mostra alguém programando, sem relação com assistência
técnica de celular. Uma ilustração do mundo da loja — bancada, aparelho aberto, ferramenta — diria
mais e dispensaria a dependência de terceiro.

### 8. Unificar os dois visuais
**Situação atual:** convivem dois sistemas de design. As telas novas (Painel, Orçamento, Estoque,
Caixa, Configuração, Login) usam Tailwind com paleta Material-3. As antigas (Lista de compras,
"Em breve") usam `wwwroot/css/site.css`, verde institucional com fonte Inter.

**O que envolve:** migrar `Views/ListaCompra/` e `Views/Shared/EmBreve.cshtml` para Tailwind,
aproveitando o partial `_HeadTailwind.cshtml`. Depois o `site.css` encolhe bastante. Cosmético.

### 9. Dashboards de verdade
**Situação atual:** `DashboardsController` retorna a tela "Em breve" e os KPIs do Painel mostram
estado vazio.

**O que envolve:** depende de Caixa, Estoque e OS no banco — sem dado gravado não há o que somar.

### 10. Autoria detalhada (auditoria mínima)
**Situação atual:** a OS já grava quem a emitiu, e as movimentações de estoque quem as fez. Uma auditoria completa, como o
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

### Docker (decisão revista — ver item 6)
Estava descartado com a premissa de rodar só local: `dotnet publish` já entrega uma pasta com
executável, sem daemon para instalar, então Docker seria peso morto. Essa premissa mudou — ver
item 6 em Alta prioridade, que reabre hospedagem na nuvem via Supabase + Hostinger, com Docker como
uma das duas rotas possíveis para subir o binário no servidor.
