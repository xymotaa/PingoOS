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

    const editarCodigo = (typeof EDITAR_CODIGO === "string") ? EDITAR_CODIGO : "";

    function parseDecimal(valor) {
        return parseFloat(String(valor).replace(/\./g, "").replace(",", ".")) || 0;
    }

    function formatarDecimal(valor) {
        return (Number(valor) || 0).toFixed(2).replace(".", ",");
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
        const conflito = produtos.some(function (p) { return p.codigo === codigo && p.codigo !== editarCodigo; });
        if (conflito) {
            irParaEtapa(1);
            npCodigo.focus();
            mostrarToast("Já existe um produto com o código \"" + codigo + "\".", true);
            return;
        }

        const dados = {
            codigo: codigo,
            nome: nome,
            categoria: npCategoria.value || "Sem categoria",
            saldoAtual: parseInt(npEstoqueInicial.value, 10) || 0,
            estoqueMinimo: parseInt(npEstoqueMinimo.value, 10) || 0,
            custoUnitario: parseDecimal(npCusto.value),
            precoVenda: parseDecimal(npPreco.value)
        };

        let flag;
        if (editarCodigo) {
            const idx = produtos.findIndex(function (p) { return p.codigo === editarCodigo; });
            if (idx >= 0) produtos[idx] = dados; else produtos.push(dados);
            flag = "editado=1";
        } else {
            produtos.push(dados);
            flag = "cadastrado=1";
        }

        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(produtos)); } catch (e) { /* ignora */ }

        window.location.href = ESTOQUE_INDEX_URL + "?" + flag;
    }

    function preencherFormulario() {
        if (!editarCodigo) return;
        const produto = lerProdutos().find(function (p) { return p.codigo === editarCodigo; });
        if (!produto) {
            mostrarToast("Produto não encontrado para edição.", true);
            return;
        }
        npNome.value = produto.nome || "";
        npCodigo.value = produto.codigo || "";
        npPreco.value = formatarDecimal(produto.precoVenda);
        npCategoria.value = produto.categoria || "";
        npEstoqueInicial.value = (produto.saldoAtual != null) ? produto.saldoAtual : "";
        npEstoqueMinimo.value = (produto.estoqueMinimo != null) ? produto.estoqueMinimo : "";
        npCusto.value = formatarDecimal(produto.custoUnitario);

        document.getElementById("npTitulo").textContent = "Editar produto";
        document.getElementById("npSubtitulo").textContent = "Edite os dados de “" + produto.nome + "”.";
        salvarNovoProdutoBtn.textContent = "Salvar alterações";
    }

    salvarNovoProdutoBtn.addEventListener("click", salvarNovoProduto);
    npAvancarEtapaBtn.addEventListener("click", function () { irParaEtapa(etapaAtual + 1); });
    npSteps.forEach(function (step) {
        step.addEventListener("click", function () { irParaEtapa(parseInt(step.dataset.step, 10)); });
    });

    preencherFormulario();
    irParaEtapa(1);
});
