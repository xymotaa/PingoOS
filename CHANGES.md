# Registro de Alterações

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

O painel usa o verde institucional (`primary #003527`), a ilustração, uma frase do que o sistema
faz e o nome da loja. Como assinatura, o ícone de chave inglesa em escala grande, cortado pela
borda inferior direita a 5% de opacidade.

### Arquivos Alterados

| Arquivo | Alteração |
|---|---|
| `Views/Shared/_PainelIlustracao.cshtml` | **novo** — painel direito compartilhado |
| `Views/Conta/Login.cshtml` | reescrita em duas colunas; identidade da loja acima do formulário |
| `Views/Conta/EsqueciSenha.cshtml` | reescrita na mesma moldura do login |
| `Views/Conta/Usuarios.cshtml` | `max-w-[1000px]` → `max-w-[1440px]` |
| `wwwroot/img/code-typing.png` | **novo** — ilustração Storyset recolorida |
| `docs/screenshots/` | login, recuperar e usuarios atualizados |

### Sobre a ilustração
O Storyset não serve o SVG do estilo *cuate* publicamente — só o PNG 600×400, no amarelo padrão
(`#FFC727`). Baixei o PNG e remapeei a família do amarelo para o verde `#357A49` pedido,
preservando as variações de tom (10.566 pixels). Como é PNG, é exibido em no máximo 420px de
largura para não perder nitidez.

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
