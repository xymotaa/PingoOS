# Pingo OS

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)
![ASP.NET Core MVC](https://img.shields.io/badge/ASP.NET%20Core-MVC-5C2D91)
![EF Core](https://img.shields.io/badge/EF%20Core-10.0.9-512BD4)
![SQLite](https://img.shields.io/badge/SQLite-loja.db-003B57?logo=sqlite&logoColor=white)
![Licença](https://img.shields.io/badge/Licença-Apache%202.0-informational)

Pingo OS é um sistema de gestão (ERP) para lojas de assistência técnica e acessórios de celular,
pensado para rodar direto no computador do estabelecimento — sem mensalidade, sem depender de
internet e sem enviar dados para servidor de terceiros.

![Painel do Pingo OS](docs/screenshots/painel.png)

## Sobre o projeto

O projeto nasceu para resolver um problema concreto: pequenas assistências técnicas costumam
gerenciar clientes, estoque e vendas em papel, planilhas soltas ou sistemas pagos caros demais
para o porte do negócio. O Pingo OS reúne isso em um único sistema, instalado localmente, com o
dono dos dados sendo sempre o dono da loja.

É software livre, aberto para uso, estudo e contribuição.

## Instalação

### Uso na loja (recomendado)

**Windows:** baixe o `PingoInstaller.exe` da [última versão publicada](https://github.com/xymotaa/PingoOS/releases/latest)
e execute — ele pede elevação automaticamente, instala .NET e Git se faltarem, publica o sistema
e registra como serviço do Windows (sobe sozinho ao ligar o computador). Rodar de novo abre um
menu com atualizar, reinstalar, ligar/desligar, resetar senha e desinstalar.

**Linux:**

```bash
git clone https://github.com/xymotaa/PingoOS.git
cd PingoOS
sudo ./install.sh
```

Instala .NET se faltar, publica em `/opt/pingo-os` e registra como serviço systemd.

Em ambos os casos o sistema fica disponível em **http://localhost:5096**, e rodar o instalador de
novo atualiza para a versão mais recente sem apagar os dados.

### Desenvolvimento

```bash
git clone https://github.com/xymotaa/PingoOS.git
cd PingoOS/ListasCompras
dotnet restore
dotnet run
```

Acesse **http://localhost:5096**. Na primeira execução o sistema cria o banco `loja.db` e aplica
as migrations automaticamente.

## Requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download) ou superior
- Windows ou Linux

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | ASP.NET Core MVC, .NET 10 |
| ORM | Entity Framework Core |
| Banco de dados | SQLite (arquivo único, sem servidor) |
| Front-end | Tailwind CSS, sem build step |

## Contribuindo

Contribuições são bem-vindas. Abra uma [issue](https://github.com/xymotaa/PingoOS/issues) para
relatar bugs ou sugerir melhorias, ou envie um pull request.

## Contato

Desenvolvido por [Lucas Barros Mota](https://github.com/xymotaa).

## Licença

Distribuído sob a **Licença Apache 2.0** — veja [LICENSE](LICENSE) para o texto completo.

Materiais de terceiros usados no projeto estão listados em [NOTICE](NOTICE).
