#!/usr/bin/env bash
# ==============================================================================
# Instalador do Pingo OS para Linux
#
# Baixe só este arquivo e rode com sudo. Ele cuida do resto: instala o que
# faltar (.NET, Git), baixa o sistema e registra como serviço systemd. Rodar
# de novo depois SEMPRE atualiza para a última versão publicada, sem apagar
# os dados da loja.
#
# Só atualiza em versões marcadas (tags no GitHub, ex: v1.0.1) — nunca o
# último commit do repositório direto. Isso existe de propósito: evita que
# uma mudança ainda em teste vire produção sozinha em todas as lojas.
# ==============================================================================
set -euo pipefail

REPO_URL="https://github.com/xymotaa/xypedidos.git"
PORTA=5096
NOME_SERVICO="pingo-os"
PASTA_BASE="/opt/pingo-os"
PASTA_CODIGO="${PASTA_BASE}/codigo"
PASTA_APP="${PASTA_BASE}/app"
URL="http://localhost:${PORTA}"

echo ""
echo "  ========================================"
echo "   PINGO OS - Instalador para Linux"
echo "  ========================================"
echo ""

if [ "$(id -u)" -ne 0 ]; then
    echo "  [ERRO] Este instalador precisa de permissão de administrador."
    echo "  Rode de novo assim: sudo ./install.sh"
    exit 1
fi

# --- Passo 1: .NET -----------------------------------------------------------
echo "  [1/5] Verificando o .NET..."
if ! command -v dotnet >/dev/null 2>&1; then
    echo "        Não encontrado. Baixando e instalando..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    rm -f /tmp/dotnet-install.sh
    echo "        .NET instalado."
else
    echo "        Já instalado, pulando."
fi

# --- Passo 2: Git --------------------------------------------------------------
echo "  [2/5] Verificando o Git..."
if ! command -v git >/dev/null 2>&1; then
    echo "        Não encontrado. Instalando..."
    if command -v apt-get >/dev/null 2>&1; then
        apt-get update -qq && apt-get install -y -qq git
    elif command -v dnf >/dev/null 2>&1; then
        dnf install -y -q git
    elif command -v yum >/dev/null 2>&1; then
        yum install -y -q git
    elif command -v pacman >/dev/null 2>&1; then
        pacman -Sy --noconfirm --quiet git
    elif command -v zypper >/dev/null 2>&1; then
        zypper --quiet install -y git
    else
        echo "  [ERRO] Não reconheci o gerenciador de pacotes desta distribuição."
        echo "        Instale o git manualmente e rode este instalador de novo."
        exit 1
    fi
    echo "        Git instalado."
else
    echo "        Já instalado, pulando."
fi

# --- Passo 3: para o serviço antes de mexer nos arquivos ---------------------
echo "  [3/5] Preparando a atualização..."
if systemctl is-active --quiet "$NOME_SERVICO" 2>/dev/null; then
    echo "        Parando o serviço atual..."
    systemctl stop "$NOME_SERVICO"
fi

mkdir -p "$PASTA_BASE"

# --- Passo 4: busca só até a última TAG publicada, nunca o commit mais recente
echo "  [4/5] Baixando a versão mais recente..."

# Um clone anterior interrompido (queda de energia, ctrl+c, disco cheio) deixa a
# pasta .git pela metade: existe mas não responde a comandos git. Confere se o
# clone é válido de verdade antes de tentar atualizar; se não for, começa do
# zero em vez de tentar consertar um clone quebrado.
CLONE_VALIDO=0
if [ -d "$PASTA_CODIGO/.git" ] && git -C "$PASTA_CODIGO" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
    CLONE_VALIDO=1
fi

if [ "$CLONE_VALIDO" -eq 0 ]; then
    if [ -d "$PASTA_CODIGO" ]; then
        echo "        Encontrei uma cópia incompleta de instalação anterior, refazendo..."
        rm -rf "$PASTA_CODIGO"
    fi
    echo "        Primeira instalação: clonando o repositório..."
    git clone --quiet "$REPO_URL" "$PASTA_CODIGO"
else
    echo "        Buscando novidades no repositório..."
    git -C "$PASTA_CODIGO" fetch --quiet --tags origin
fi

# A última tag por ordem de criação é a versão publicada mais recente.
# Sem nenhuma tag ainda no repositório, cai no HEAD do main como reserva.
ULTIMA_TAG="$(git -C "$PASTA_CODIGO" tag --sort=-creatordate | head -n1 || true)"
if [ -n "$ULTIMA_TAG" ]; then
    echo "        Instalando a versão ${ULTIMA_TAG}..."
    git -C "$PASTA_CODIGO" checkout --quiet "$ULTIMA_TAG"
else
    echo "        Nenhuma versão marcada ainda; usando a mais recente do repositório."
    git -C "$PASTA_CODIGO" checkout --quiet main
    git -C "$PASTA_CODIGO" pull --quiet origin main
fi

PASTA_PROJETO="$PASTA_CODIGO/ListasCompras"
if [ ! -f "$PASTA_PROJETO/ListasCompras.csproj" ]; then
    echo "  [ERRO] O repositório baixado não tem ListasCompras/ListasCompras.csproj."
    exit 1
fi

# Limpa obj/ e bin/ antes de publicar: evita erro de permissão quando esses
# arquivos foram criados por outro usuário antes deste instalador rodar
rm -rf "$PASTA_PROJETO/obj" "$PASTA_PROJETO/bin"

# --- Passo 5: publica preservando o banco, registra o serviço ----------------
echo "  [5/5] Instalando o sistema..."
mkdir -p "$PASTA_APP"

BACKUP_BANCO=""
if [ -f "$PASTA_APP/loja.db" ]; then
    BACKUP_BANCO="$(mktemp -d)/loja.db"
    cp "$PASTA_APP/loja.db" "$BACKUP_BANCO"
fi

dotnet publish "$PASTA_PROJETO" -c Release -o "$PASTA_APP" --nologo -v q

if [ -n "$BACKUP_BANCO" ]; then
    cp "$BACKUP_BANCO" "$PASTA_APP/loja.db"
    rm -rf "$(dirname "$BACKUP_BANCO")"
fi

cat > "/etc/systemd/system/${NOME_SERVICO}.service" <<EOF
[Unit]
Description=Pingo OS
After=network.target

[Service]
WorkingDirectory=${PASTA_APP}
ExecStart=${PASTA_APP}/ListasCompras
Restart=on-failure
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ROOT=/usr/share/dotnet

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "$NOME_SERVICO" >/dev/null
systemctl restart "$NOME_SERVICO"

echo "        Aguardando o sistema subir..."
for _ in $(seq 1 20); do
    if curl -sf "$URL" >/dev/null 2>&1; then
        break
    fi
    sleep 1
done

if command -v xdg-open >/dev/null 2>&1 && [ -n "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then
    xdg-open "$URL" >/dev/null 2>&1 || true
fi

echo ""
echo "  ========================================"
echo "   Pronto! O Pingo OS está rodando."
echo "  ========================================"
echo ""
echo "  Endereço: $URL"
echo "  Ele inicia sozinho toda vez que o computador ligar."
echo ""
echo "  Para atualizar no futuro, baixe este mesmo install.sh de novo"
echo "  e rode com sudo — ele busca a versão mais recente sozinho."
echo ""
echo "  Comandos úteis:"
echo "    sudo systemctl status ${NOME_SERVICO}   # ver se está rodando"
echo "    sudo systemctl restart ${NOME_SERVICO}  # reiniciar"
echo "    sudo journalctl -u ${NOME_SERVICO} -f   # ver o que está acontecendo"
