# Registro de Alterações

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
