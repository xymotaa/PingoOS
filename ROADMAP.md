# Melhorias futuras

O que ainda não existe no Pingo OS, por que faz falta e o que envolve fazer. Não é uma promessa
de entrega nem tem prazo — é a lista do que já foi discutido e decidido adiar, para não se perder.

Quando um item for feito, tire daqui e registre no [CHANGES.md](CHANGES.md).

---

## Alta prioridade

### Backup do banco pela tela de Configuração
**Falta porque** hoje o backup depende de a pessoa saber copiar `loja.db` pelo terminal — ou seja,
na prática não acontece. Um dono de loja não vai fazer isso, e o banco tem o cadastro de clientes,
ordens de serviço e histórico.

**O que envolve:** um botão em Configurações que devolve o `loja.db` como download, e outro que
restaura a partir de um arquivo enviado. A parte delicada é a restauração: precisa fechar as
conexões do EF Core antes de sobrescrever o arquivo, ou o SQLite recusa.

### Persistir Orçamento, Estoque e Caixa no banco
**Falta porque** os três módulos já têm tela pronta mas não gravam nada. O Estoque usa
`localStorage` como ponte temporária (chave `xyEstoqueProdutos`), o que significa que os dados
somem se o usuário limpar o navegador e não existem em outra máquina.

**O que envolve:** modelar as entidades seguindo o padrão da Lista de Compras, criar as migrations
e trocar os dados de exemplo dos controllers por consultas reais. É o maior item da lista e o que
mais muda a utilidade do sistema.

### Registrar quem emitiu cada Ordem de Serviço
**Falta porque** a OS impressa tem a linha "Responsável Técnico", mas o sistema não sabe quem foi.
Com o login já existente, dá para preencher automaticamente com o nome de quem está logado.

**O que envolve:** depende do item acima (Orçamento no banco). É o motivo pelo qual usuários são
desativados em vez de excluídos — apagar um técnico deixaria ordens órfãs.

---

## Média prioridade

### Notificação ao cliente (e-mail ou WhatsApp)
**Falta porque** os termos da OS dizem que, para considerar um aparelho abandonado, o cliente
precisa ter sido **notificado por escrito**. Hoje o sistema não produz essa prova. Isso deixa de
ser conveniência e passa a ser o que sustenta juridicamente a cláusula 8 dos termos — além de
resolver o "seu aparelho está pronto".

**O que envolve:** uma tela de configuração de SMTP, uma tabela de notificações enviadas (com data
e destinatário, que é a prova) e um `BackgroundService` para reenviar as que falharem.

> Nota de stack: **não precisamos de cron.** O `BackgroundService` do .NET roda dentro da própria
> aplicação. Sistemas em PHP precisam de duas linhas no crontab do servidor para a mesma coisa.

### Recuperação de senha
**Falta porque** depende do envio de e-mail acima. Hoje quem esquece a senha depende do
administrador redefinir, e o administrador que esquecer a própria fica trancado do lado de fora —
só mexendo direto no banco.

**O que envolve:** token de uso único com validade curta, gravado no banco, e a tela de troca.
Só faz sentido depois que o e-mail funcionar.

### Script de instalação para o usuário final
**Falta porque** o público do sistema é dono de loja, não desenvolvedor. Hoje instalar exige clonar
o repositório, ter o SDK do .NET e rodar comandos no terminal. Essa é a lição que o
[MapOS](https://github.com/RamonSilva20/mapos) acerta: sem instalação simples, o sistema não chega
em quem precisa dele.

**O que envolve:** um `install.sh` (Linux) e um `install.bat` (Windows) que instalam o runtime,
publicam a aplicação, registram como serviço do sistema (systemd / Serviço do Windows) e abrem o
navegador. Fica bem mais simples que no MapOS porque não há PHP, MySQL nem webserver para
configurar — o .NET publica uma pasta com um executável e o SQLite é um arquivo.

---

## Baixa prioridade

### Unificar os dois visuais
**Situação atual:** convivem dois sistemas de design. As telas novas (Painel, Orçamento, Estoque,
Caixa, Configuração, Login) usam Tailwind via CDN com a paleta Material-3. As telas antigas
(Lista de compras, "Em breve") usam `wwwroot/css/site.css`, verde institucional com fonte Inter.

**O que envolve:** migrar `Views/ListaCompra/` e `Views/Shared/EmBreve.cshtml` para o padrão
Tailwind, aproveitando o partial `_HeadTailwind.cshtml`. Depois disso o `site.css` pode encolher
bastante. É trabalho cosmético — não muda nada de funcionalidade.

### Dashboards de verdade
**Situação atual:** `DashboardsController` retorna a tela genérica "Em breve", e os KPIs do Painel
mostram estado vazio.

**O que envolve:** depende de Caixa e Estoque estarem no banco — sem dado gravado não há o que
somar.

---

## Descartado (e por quê)

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
