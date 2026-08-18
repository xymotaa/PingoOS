@echo off
setlocal enabledelayedexpansion
title Instalador do Pingo OS
color 0A

REM ============================================================================
REM  Instalador do Pingo OS para Windows
REM
REM  Baixe so este arquivo e rode como Administrador. Ele cuida do resto:
REM  instala o que faltar (.NET, Git), baixa o sistema e registra como
REM  Servico do Windows. Rodar de novo depois SEMPRE atualiza para a ultima
REM  versao publicada, sem apagar os dados da loja.
REM
REM  So atualiza em versoes marcadas (tags no GitHub, ex: v1.0.1) - nunca o
REM  ultimo commit do repositorio direto. Isso existe de proposito: evita que
REM  uma mudanca ainda em teste vire producao sozinha em todas as lojas.
REM ============================================================================

set REPO_URL=https://github.com/xymotaa/xypedidos.git
set PORTA=5096
set NOME_SERVICO=PingoOS
set PASTA_BASE=%ProgramFiles%\PingoOS
set PASTA_CODIGO=%PASTA_BASE%\codigo
set PASTA_APP=%PASTA_BASE%\app
set URL=http://localhost:%PORTA%

cls
echo.
echo   ========================================
echo    PINGO OS - Instalador para Windows
echo   ========================================
echo.

REM --- Precisa ser Administrador --------------------------------------------
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo   [ERRO] Este instalador precisa ser aberto como Administrador.
    echo.
    echo   Clique com o botao direito neste arquivo e escolha
    echo   "Executar como administrador", depois rode de novo.
    goto :erro_final
)

REM --- Passo 1: .NET ----------------------------------------------------------
echo   [1/5] Verificando o .NET...
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo         Nao encontrado. Baixando e instalando...
    powershell -NoProfile -Command "Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile $env:TEMP\dotnet-install.ps1" >nul
    powershell -NoProfile -ExecutionPolicy Bypass -File "%TEMP%\dotnet-install.ps1" -Channel 10.0 -InstallDir "%ProgramFiles%\dotnet" >nul
    setx PATH "%PATH%;%ProgramFiles%\dotnet" /M >nul
    set "PATH=%PATH%;%ProgramFiles%\dotnet"
    del "%TEMP%\dotnet-install.ps1" >nul 2>&1
    echo         .NET instalado.
) else (
    echo         Ja instalado, pulando.
)

REM --- Passo 2: Git -------------------------------------------------------------
echo   [2/5] Verificando o Git...
where git >nul 2>&1
if %errorlevel% neq 0 (
    echo         Nao encontrado. Instalando via winget...
    where winget >nul 2>&1
    if %errorlevel% equ 0 (
        winget install --id Git.Git -e --source winget --accept-package-agreements --accept-source-agreements --silent >nul 2>&1
    )
    REM winget pode nao existir em Windows mais antigos, ou a instalacao falhar;
    REM o instalador oficial do Git e o caminho que sempre funciona
    where git >nul 2>&1
    if %errorlevel% neq 0 (
        echo         winget indisponivel, baixando o instalador oficial do Git...
        powershell -NoProfile -Command "Invoke-WebRequest -Uri https://github.com/git-for-windows/git/releases/latest/download/Git-64-bit.exe -OutFile $env:TEMP\git-installer.exe" >nul
        "%TEMP%\git-installer.exe" /VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS /COMPONENTS="icons,ext\reg\shellhere,assoc,assoc_sh"
        del "%TEMP%\git-installer.exe" >nul 2>&1
        set "PATH=%PATH%;%ProgramFiles%\Git\cmd"
    )
    where git >nul 2>&1
    if %errorlevel% neq 0 (
        echo   [ERRO] Nao foi possivel instalar o Git automaticamente.
        echo         Instale manualmente em https://git-scm.com/download/win e rode este instalador de novo.
        goto :erro_final
    )
    echo         Git instalado.
) else (
    echo         Ja instalado, pulando.
)

REM --- Passo 3: para o servico antes de mexer nos arquivos ---------------------
echo   [3/5] Preparando a atualizacao...
sc query "%NOME_SERVICO%" >nul 2>&1
if %errorlevel% equ 0 (
    echo         Parando o servico atual...
    net stop "%NOME_SERVICO%" >nul 2>&1
)

if not exist "%PASTA_BASE%" mkdir "%PASTA_BASE%"

REM --- Passo 4: busca so ate a ultima TAG publicada, nunca o commit mais recente
echo   [4/5] Baixando a versao mais recente...

REM Um clone anterior interrompido (energia, antivirus, cancelamento) deixa a pasta
REM .git pela metade: existe mas nao responde a comandos git. "git rev-parse" confere
REM se o clone e valido de verdade, nao so se a pasta existe; se nao for, comeca do
REM zero em vez de tentar consertar um clone quebrado.
set CLONE_VALIDO=0
if exist "%PASTA_CODIGO%\.git" (
    git -C "%PASTA_CODIGO%" rev-parse --is-inside-work-tree >nul 2>&1
    if !errorlevel! equ 0 set CLONE_VALIDO=1
)

if "!CLONE_VALIDO!"=="0" (
    if exist "%PASTA_CODIGO%" (
        echo         Encontrei uma copia incompleta de instalacao anterior, refazendo...
        rmdir /s /q "%PASTA_CODIGO%"
    )
    echo         Primeira instalacao: clonando o repositorio...
    git clone --quiet "%REPO_URL%" "%PASTA_CODIGO%"
    if !errorlevel! neq 0 (
        echo   [ERRO] Nao foi possivel baixar o sistema. Confira sua conexao com a internet.
        goto :erro_final
    )
) else (
    echo         Buscando novidades no repositorio...
    git -C "%PASTA_CODIGO%" fetch --quiet --tags origin
    if !errorlevel! neq 0 (
        echo   [ERRO] Nao foi possivel buscar atualizacoes. Confira sua conexao com a internet.
        goto :erro_final
    )
)

REM A ultima tag por ordem de criacao é a versão publicada mais recente.
REM Sem nenhuma tag ainda no repositório, cai no HEAD do main como reserva.
for /f "delims=" %%v in ('git -C "%PASTA_CODIGO%" tag --sort=-creatordate 2^>nul') do (
    set ULTIMA_TAG=%%v
    goto :tag_encontrada
)
:tag_encontrada
if defined ULTIMA_TAG (
    echo         Instalando a versao %ULTIMA_TAG%...
    git -C "%PASTA_CODIGO%" checkout --quiet "%ULTIMA_TAG%"
) else (
    echo         Nenhuma versao marcada ainda; usando a mais recente do repositorio.
    git -C "%PASTA_CODIGO%" checkout --quiet main
    git -C "%PASTA_CODIGO%" pull --quiet origin main
)

set PASTA_PROJETO=%PASTA_CODIGO%\ListasCompras
if not exist "%PASTA_PROJETO%\ListasCompras.csproj" (
    echo   [ERRO] O repositorio baixado nao tem ListasCompras\ListasCompras.csproj.
    goto :erro_final
)

REM Limpa obj\ e bin\ antes de publicar: evita "Access to the path is denied"
REM quando esses arquivos foram criados por outro usuario/permissao antes
if exist "%PASTA_PROJETO%\obj" rmdir /s /q "%PASTA_PROJETO%\obj"
if exist "%PASTA_PROJETO%\bin" rmdir /s /q "%PASTA_PROJETO%\bin"

REM --- Passo 5: publica preservando o banco, registra o servico ----------------
echo   [5/5] Instalando o sistema...
if not exist "%PASTA_APP%" mkdir "%PASTA_APP%"

if exist "%PASTA_APP%\loja.db" (
    copy /y "%PASTA_APP%\loja.db" "%TEMP%\loja.db.bak" >nul
)

dotnet publish "%PASTA_PROJETO%" -c Release -o "%PASTA_APP%" --nologo -v q
if %errorlevel% neq 0 (
    echo   [ERRO] Falha ao publicar o sistema. Veja a mensagem acima.
    echo.
    echo   Se o erro falar em "Access to the path is denied", apague as pastas
    echo   obj e bin em "%PASTA_PROJETO%" e rode o instalador de novo.
    goto :erro_final
)

if exist "%TEMP%\loja.db.bak" (
    copy /y "%TEMP%\loja.db.bak" "%PASTA_APP%\loja.db" >nul
    del "%TEMP%\loja.db.bak" >nul 2>&1
)

sc query "%NOME_SERVICO%" >nul 2>&1
if %errorlevel% equ 0 (
    sc config "%NOME_SERVICO%" binPath= "\"%PASTA_APP%\ListasCompras.exe\"" >nul
) else (
    sc create "%NOME_SERVICO%" binPath= "\"%PASTA_APP%\ListasCompras.exe\"" start= auto DisplayName= "Pingo OS" >nul
    sc failure "%NOME_SERVICO%" reset= 86400 actions= restart/5000/restart/5000/restart/5000 >nul
)

net start "%NOME_SERVICO%" >nul 2>&1

echo         Aguardando o sistema subir...
set TENTATIVAS=0
:esperar
set /a TENTATIVAS+=1
powershell -NoProfile -Command "try { (Invoke-WebRequest -Uri '%URL%' -UseBasicParsing -TimeoutSec 1) | Out-Null; exit 0 } catch { exit 1 }" >nul 2>&1
if %errorlevel% equ 0 goto pronto
if %TENTATIVAS% geq 20 goto pronto
timeout /t 1 /nobreak >nul
goto esperar

:pronto
start "" "%URL%"

echo.
echo   ========================================
echo    Pronto! O Pingo OS esta rodando.
echo   ========================================
echo.
echo   Endereco: %URL%
echo   Ele inicia sozinho toda vez que o computador ligar.
echo.
echo   Para atualizar no futuro, baixe este mesmo install.bat de novo
echo   e rode como Administrador - ele busca a versao mais recente sozinho.
echo.
echo   Comandos uteis (Prompt como Administrador):
echo     sc query %NOME_SERVICO%     - ver se esta rodando
echo     net stop %NOME_SERVICO%     - parar
echo     net start %NOME_SERVICO%    - iniciar
echo.
pause
exit /b 0

:erro_final
echo.
echo   ========================================
echo    A instalacao parou por causa de um erro
echo   ========================================
echo.
echo   Revise a mensagem acima. Esta janela so fecha quando voce apertar uma tecla.
echo.
pause
exit /b 1
