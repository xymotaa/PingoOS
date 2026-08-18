document.addEventListener("DOMContentLoaded", function () {
    const filtroBusca = document.getElementById("filtroBusca");
    const filtroStatus = document.getElementById("filtroStatus");
    const toggleFiltrosBtn = document.getElementById("toggleFiltrosBtn");
    const filtrosExtras = document.getElementById("filtrosExtras");
    const linhas = Array.prototype.slice.call(document.querySelectorAll(".linha-produto"));
    const vazioState = document.getElementById("vazioState");
    const imprimirBtn = document.getElementById("imprimirBtn");

    // ===== Filtro da lista (a tabela vem pronta do servidor) =====

    function filtrar() {
        const termo = (filtroBusca ? filtroBusca.value : "").trim().toLowerCase();
        const status = filtroStatus ? filtroStatus.value : "";
        let visiveis = 0;

        linhas.forEach(function (linha) {
            const combina = (!termo || linha.dataset.busca.indexOf(termo) !== -1)
                         && (!status || linha.dataset.situacao === status);
            linha.classList.toggle("hidden", !combina);
            if (combina) visiveis++;
        });

        if (vazioState && linhas.length > 0) vazioState.classList.toggle("hidden", visiveis > 0);
    }

    if (filtroBusca) filtroBusca.addEventListener("input", filtrar);
    if (filtroStatus) filtroStatus.addEventListener("change", filtrar);

    linhas.forEach(function (linha) {
        if (!linha.dataset.href) return;
        linha.addEventListener("click", function () { window.location.href = linha.dataset.href; });
    });
    if (toggleFiltrosBtn && filtrosExtras) {
        toggleFiltrosBtn.addEventListener("click", function () { filtrosExtras.classList.toggle("hidden"); });
    }
    if (imprimirBtn) imprimirBtn.addEventListener("click", function () { window.print(); });

    // ===== Movimentação (entrada / saída) =====

    const modal = document.getElementById("modalMovimentacao");
    const movProdutoId = document.getElementById("movProdutoId");
    const movProdutoNome = document.getElementById("movProdutoNome");
    const movQtdValor = document.getElementById("movQtdValor");
    const movQuantidade = document.getElementById("movQuantidade");
    const movSaldoInfo = document.getElementById("movSaldoInfo");
    const fecharBtn = document.getElementById("fecharMovimentacaoBtn");

    let saldoAtual = 0;
    let quantidade = 1;

    function tipoSelecionado() {
        const marcado = document.querySelector('input[name="movTipo"]:checked');
        return marcado ? marcado.value : "entrada";
    }

    function atualizarPrevia() {
        movQtdValor.textContent = quantidade;
        movQuantidade.value = quantidade;
        const resultado = tipoSelecionado() === "saida" ? saldoAtual - quantidade : saldoAtual + quantidade;
        movSaldoInfo.textContent = "Saldo atual: " + saldoAtual + " → ficará com " + resultado + ".";
        movSaldoInfo.classList.toggle("text-error", resultado < 0);
    }

    // Chamada pelo botão de cada linha da tabela
    window.abrirMovimentacao = function (id, nome, saldo) {
        movProdutoId.value = id;
        movProdutoNome.textContent = nome;
        saldoAtual = saldo;
        quantidade = 1;
        atualizarPrevia();
        modal.classList.remove("hidden");
    };

    function fechar() { modal.classList.add("hidden"); }

    if (fecharBtn) fecharBtn.addEventListener("click", fechar);
    if (modal) {
        modal.addEventListener("click", function (e) { if (e.target === modal) fechar(); });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && !modal.classList.contains("hidden")) fechar();
        });
    }

    const menos = document.getElementById("movQtdMenos");
    const mais = document.getElementById("movQtdMais");
    if (menos) menos.addEventListener("click", function () { if (quantidade > 1) quantidade--; atualizarPrevia(); });
    if (mais) mais.addEventListener("click", function () { quantidade++; atualizarPrevia(); });

    document.querySelectorAll('input[name="movTipo"]').forEach(function (radio) {
        radio.addEventListener("change", function () {
            document.querySelectorAll(".mov-tipo-opcao").forEach(function (label) {
                const ativo = label.dataset.tipo === tipoSelecionado();
                label.classList.toggle("border-secondary", ativo);
                label.classList.toggle("bg-surface-container-low", ativo);
            });
            atualizarPrevia();
        });
    });
});
