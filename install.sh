#!/usr/bin/env bash
# Instala o Pingo OS como serviço do sistema (systemd) na máquina da loja.
# Pensado para quem nunca usou terminal: cada passo avisa o que está fazendo.
set -euo pipefail

PORTA=5096
NOME_SERVICO="pingo-os"
PASTA_INSTALACAO="/opt/pingo-os"
URL="http://localhost:${PORTA}"

echo "=== Instalando o Pingo OS ==="

if [ "$(id -u)" -ne 0 ]; then
    echo "Este instalador precisa de permissão de administrador."
    echo "Rode de novo assim: sudo ./install.sh"
    exit 1
fi

DIR_SCRIPT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PASTA_PROJETO="$DIR_SCRIPT/ListasCompras"

if [ ! -f "$PASTA_PROJETO/ListasCompras.csproj" ]; then
    echo "Não encontrei ListasCompras/ListasCompras.csproj ao lado deste script."
    echo "Rode o install.sh de dentro da pasta onde o projeto foi baixado."
    exit 1
fi

# 1. Runtime do .NET, só se ainda não tiver
if ! command -v dotnet >/dev/null 2>&1; then
    echo "--- Instalando o .NET (o sistema roda em cima dele) ---"
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    bash /tmp/dotnet-install.sh --channel 10.0 --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    rm -f /tmp/dotnet-install.sh
else
    echo "--- .NET já instalado, pulando ---"
fi

# 2. Limpa obj/ e bin/ do projeto antes de publicar. Se alguém já abriu o projeto sem
#    ser root (rodou dotnet build/run direto) antes de rodar este instalador (que roda
#    com sudo), esses arquivos intermediários ficam com outro dono e o dotnet publish
#    pode falhar por permissão na build seguinte.
rm -rf "$PASTA_PROJETO/obj" "$PASTA_PROJETO/bin"

# 3. Publica o sistema numa pasta fixa, fora da pasta baixada — assim atualizar
#    (baixar de novo e rodar o install.sh outra vez) não deixa lixo de versão antiga.
#    O banco sai do caminho antes do publish e volta depois: dotnet publish limpa a
#    pasta de destino, e os dados da loja não podem virar vítima de uma atualização.
echo "--- Publicando o sistema em $PASTA_INSTALACAO ---"
BACKUP_BANCO=""
if [ -f "$PASTA_INSTALACAO/loja.db" ]; then
    echo "--- Guardando o banco da instalação anterior ---"
    BACKUP_BANCO="$(mktemp -d)/loja.db"
    cp "$PASTA_INSTALACAO/loja.db" "$BACKUP_BANCO"
fi

mkdir -p "$PASTA_INSTALACAO"
dotnet publish "$PASTA_PROJETO" -c Release -o "$PASTA_INSTALACAO" --nologo

if [ -n "$BACKUP_BANCO" ]; then
    cp "$BACKUP_BANCO" "$PASTA_INSTALACAO/loja.db"
    rm -rf "$(dirname "$BACKUP_BANCO")"
fi

# 4. Serviço systemd: sobe sozinho no boot e reinicia se cair
echo "--- Registrando o serviço do sistema ---"
cat > "/etc/systemd/system/${NOME_SERVICO}.service" <<EOF
[Unit]
Description=Pingo OS
After=network.target

[Service]
WorkingDirectory=${PASTA_INSTALACAO}
ExecStart=${PASTA_INSTALACAO}/ListasCompras
Restart=on-failure
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ROOT=/usr/share/dotnet

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable "$NOME_SERVICO" >/dev/null
systemctl restart "$NOME_SERVICO"

echo "--- Aguardando o sistema subir ---"
for _ in $(seq 1 20); do
    if curl -sf "$URL" >/dev/null 2>&1; then
        break
    fi
    sleep 1
done

# 5. Abre o navegador, se houver um ambiente gráfico
if command -v xdg-open >/dev/null 2>&1 && [ -n "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then
    xdg-open "$URL" >/dev/null 2>&1 || true
fi

echo ""
echo "=== Pronto ==="
echo "O Pingo OS está rodando em $URL"
echo "Ele inicia sozinho toda vez que o computador ligar."
echo ""
echo "Comandos úteis:"
echo "  sudo systemctl status ${NOME_SERVICO}   # ver se está rodando"
echo "  sudo systemctl restart ${NOME_SERVICO}  # reiniciar"
echo "  sudo journalctl -u ${NOME_SERVICO} -f   # ver o que está acontecendo"
