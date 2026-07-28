document.addEventListener("DOMContentLoaded", function () {
    const STORAGE_KEY = "xyEstoqueProdutos";

    const salvarNovoProdutoBtn = document.getElementById("salvarNovoProdutoBtn");
    const npAvancarEtapaBtn = document.getElementById("npAvancarEtapaBtn");
    const npSteps = document.querySelectorAll(".np-step");
    const npPanels = document.querySelectorAll("[data-step-panel]");
    const npNome = document.getElementById("npNome");
    const npCodigo = document.getElementById("npCodigo");
    const npPreco = document.getElementById("npPreco");
    const npCategoria = document.getElementById("npCategoria");
    const npEstoqueInicial = document.getElementById("npEstoqueInicial");
    const npEstoqueMinimo = document.getElementById("npEstoqueMinimo");
    const npCusto = document.getElementById("npCusto");
    const toast = document.getElementById("toast");
    const toastMsg = document.getElementById("toastMsg");
    const toastIcon = document.getElementById("toastIcon");

    const TOTAL_ETAPAS = 6;
    let etapaAtual = 1;

    function parseDecimal(valor) {
        return parseFloat(String(valor).replace(/\./g, "").replace(",", ".")) || 0;
    }

    function mostrarToast(mensagem, erro) {
        toastMsg.textContent = mensagem;
        toast.classList.remove("hidden", "bg-secondary-container", "text-on-secondary-container", "bg-error-container", "text-error");
        toast.classList.add.apply(toast.classList, erro ? ["bg-error-container", "text-error"] : ["bg-secondary-container", "text-on-secondary-container"]);
        toastIcon.textContent = erro ? "error" : "check_circle";
        window.clearTimeout(mostrarToast._timer);
        mostrarToast._timer = window.setTimeout(function () { toast.classList.add("hidden"); }, 3500);
    }

    function irParaEtapa(n) {
        etapaAtual = Math.min(Math.max(n, 1), TOTAL_ETAPAS);
        npSteps.forEach(function (step) {
            const ativo = parseInt(step.dataset.step, 10) === etapaAtual;
            step.classList.toggle("text-secondary", ativo);
            step.classList.toggle("font-semibold", ativo);
            step.classList.toggle("border-secondary", ativo);
            step.classList.toggle("text-on-surface-variant", !ativo);
            step.classList.toggle("border-transparent", !ativo);
        });
        npPanels.forEach(function (panel) {
            panel.classList.toggle("hidden", parseInt(panel.dataset.stepPanel, 10) !== etapaAtual);
        });
        npAvancarEtapaBtn.classList.toggle("hidden", etapaAtual === TOTAL_ETAPAS);
    }

    function lerProdutos() {
        try {
            const salvos = JSON.parse(localStorage.getItem(STORAGE_KEY) || "null");
            if (Array.isArray(salvos)) return salvos;
        } catch (e) { /* ignora */ }
        return [];
    }

    function salvarNovoProduto() {
        const nome = npNome.value.trim();
        if (!nome) {
            irParaEtapa(1);
            npNome.focus();
            mostrarToast("Informe o nome do produto para continuar.", true);
            return;
        }

        const produtos = lerProdutos();

        let codigo = npCodigo.value.trim();
        if (!codigo) codigo = "P" + Date.now().toString().slice(-8);
        if (produtos.some(function (p) { return p.codigo === codigo; })) {
            irParaEtapa(1);
            npCodigo.focus();
            mostrarToast("Já existe um produto com o código \"" + codigo + "\".", true);
            return;
        }

        produtos.push({
            codigo: codigo,
            nome: nome,
            categoria: npCategoria.value || "Sem categoria",
            saldoAtual: parseInt(npEstoqueInicial.value, 10) || 0,
            estoqueMinimo: parseInt(npEstoqueMinimo.value, 10) || 0,
            custoUnitario: parseDecimal(npCusto.value),
            precoVenda: parseDecimal(npPreco.value)
        });

        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(produtos)); } catch (e) { /* ignora */ }

        window.location.href = ESTOQUE_INDEX_URL + "?cadastrado=1";
    }

    salvarNovoProdutoBtn.addEventListener("click", salvarNovoProduto);
    npAvancarEtapaBtn.addEventListener("click", function () { irParaEtapa(etapaAtual + 1); });
    npSteps.forEach(function (step) {
        step.addEventListener("click", function () { irParaEtapa(parseInt(step.dataset.step, 10)); });
    });

    irParaEtapa(1);
});
