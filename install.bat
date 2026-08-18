@echo off
setlocal enabledelayedexpansion
REM Instala o Pingo OS como Servico do Windows na maquina da loja.
REM Pensado para quem nunca usou terminal: cada passo avisa o que esta fazendo.

set PORTA=5096
set NOME_SERVICO=PingoOS
set PASTA_INSTALACAO=%ProgramFiles%\PingoOS
set URL=http://localhost:%PORTA%

echo === Instalando o Pingo OS ===

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Este instalador precisa ser aberto como Administrador.
    echo Clique com o botao direito em install.bat e escolha "Executar como administrador".
    pause
    exit /b 1
)

set PASTA_PROJETO=%~dp0ListasCompras

if not exist "%PASTA_PROJETO%\ListasCompras.csproj" (
    echo Nao encontrei ListasCompras\ListasCompras.csproj ao lado deste script.
    echo Rode o install.bat de dentro da pasta onde o projeto foi baixado.
    pause
    exit /b 1
)

REM 1. Runtime do .NET, so se ainda nao tiver
where dotnet >nul 2>&1
if %errorlevel% neq 0 (
    echo --- Instalando o .NET ^(o sistema roda em cima dele^) ---
    powershell -NoProfile -Command "Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile $env:TEMP\dotnet-install.ps1"
    powershell -NoProfile -ExecutionPolicy Bypass -File "%TEMP%\dotnet-install.ps1" -Channel 10.0 -InstallDir "%ProgramFiles%\dotnet"
    setx PATH "%PATH%;%ProgramFiles%\dotnet" /M >nul
    set "PATH=%PATH%;%ProgramFiles%\dotnet"
    del "%TEMP%\dotnet-install.ps1"
) else (
    echo --- .NET ja instalado, pulando ---
)

REM 2. Para o servico antigo antes de publicar por cima, senao o executavel fica travado
sc query "%NOME_SERVICO%" >nul 2>&1
if %errorlevel% equ 0 (
    echo --- Parando o servico para atualizar ---
    net stop "%NOME_SERVICO%" >nul 2>&1
)

REM 3. Limpa obj\ e bin\ do projeto antes de publicar. Se alguem ja abriu o projeto no
REM    Visual Studio/VS Code sem ser Administrador antes de rodar este instalador (que
REM    roda elevado), esses arquivos intermediarios ficam com permissao de outro contexto
REM    e o dotnet publish falha com "Access to the path is denied" na build seguinte.
if exist "%PASTA_PROJETO%\obj" rmdir /s /q "%PASTA_PROJETO%\obj"
if exist "%PASTA_PROJETO%\bin" rmdir /s /q "%PASTA_PROJETO%\bin"

REM 4. Publica numa pasta fixa. O banco sai do caminho antes e volta depois: dotnet
REM    publish limpa a pasta de destino, e os dados da loja nao podem virar vitima
REM    de uma atualizacao.
echo --- Publicando o sistema em %PASTA_INSTALACAO% ---
if exist "%PASTA_INSTALACAO%\loja.db" (
    echo --- Guardando o banco da instalacao anterior ---
    copy /y "%PASTA_INSTALACAO%\loja.db" "%TEMP%\loja.db.bak" >nul
)

if not exist "%PASTA_INSTALACAO%" mkdir "%PASTA_INSTALACAO%"
dotnet publish "%PASTA_PROJETO%" -c Release -o "%PASTA_INSTALACAO%" --nologo
if %errorlevel% neq 0 (
    echo Falha ao publicar o sistema. Confira a mensagem acima.
    echo.
    echo Se o erro falar em "Access to the path is denied", tente apagar as pastas
    echo obj e bin dentro de ListasCompras e rodar o instalador de novo.
    pause
    exit /b 1
)

if exist "%TEMP%\loja.db.bak" (
    copy /y "%TEMP%\loja.db.bak" "%PASTA_INSTALACAO%\loja.db" >nul
    del "%TEMP%\loja.db.bak"
)

REM 5. Servico do Windows: sobe sozinho no boot e reinicia se cair.
REM    O executavel roda como servico de verdade (Microsoft.Extensions.Hosting.WindowsServices
REM    no Program.cs), nao um processo solto tentando se passar por um.
sc query "%NOME_SERVICO%" >nul 2>&1
if %errorlevel% equ 0 (
    echo --- Atualizando o registro do servico ---
    sc config "%NOME_SERVICO%" binPath= "\"%PASTA_INSTALACAO%\ListasCompras.exe\"" >nul
) else (
    echo --- Registrando o servico do sistema ---
    sc create "%NOME_SERVICO%" binPath= "\"%PASTA_INSTALACAO%\ListasCompras.exe\"" start= auto DisplayName= "Pingo OS" >nul
    sc failure "%NOME_SERVICO%" reset= 86400 actions= restart/5000/restart/5000/restart/5000 >nul
)

net start "%NOME_SERVICO%" >nul 2>&1

echo --- Aguardando o sistema subir ---
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
echo === Pronto ===
echo O Pingo OS esta rodando em %URL%
echo Ele inicia sozinho toda vez que o computador ligar.
echo.
echo Comandos uteis (Prompt como Administrador):
echo   sc query %NOME_SERVICO%     - ver se esta rodando
echo   net stop %NOME_SERVICO%     - parar
echo   net start %NOME_SERVICO%    - iniciar
pause
