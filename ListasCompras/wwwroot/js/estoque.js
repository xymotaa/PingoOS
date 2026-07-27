document.addEventListener("DOMContentLoaded", function () {
    const produtos = PRODUTOS_ESTOQUE.map(function (p) { return Object.assign({}, p); });
    const selecionados = new Set();

    const estoqueBody = document.getElementById("estoqueBody");
    const vazioState = document.getElementById("vazioState");
    const filtroBusca = document.getElementById("filtroBusca");
    const filtroStatus = document.getElementById("filtroStatus");
    const toggleFiltrosBtn = document.getElementById("toggleFiltrosBtn");
    const filtrosExtras = document.getElementById("filtrosExtras");
    const chipsFiltro = document.getElementById("chipsFiltro");
    const chipsFiltroTexto = document.getElementById("chipsFiltroTexto");
    const limparFiltrosBtn = document.getElementById("limparFiltrosBtn");
    const selecionarTodosCheckbox = document.getElementById("selecionarTodosCheckbox");
    const selecionadosInfo = document.getElementById("selecionadosInfo");
    const atualizarListaBtn = document.getElementById("atualizarListaBtn");
    const excluirSelecionadosBtn = document.getElementById("excluirSelecionadosBtn");
    const imprimirBtn = document.getElementById("imprimirBtn");

    const resumoTotalProdutos = document.getElementById("resumoTotalProdutos");
    const resumoValorEstoque = document.getElementById("resumoValorEstoque");
    const resumoBaixo = document.getElementById("resumoBaixo");
    const resumoEsgotado = document.getElementById("resumoEsgotado");
    const toast = document.getElementById("toast");
    const toastMsg = document.getElementById("toastMsg");
    const toastIcon = document.getElementById("toastIcon");

    const novoProdutoBtn = document.getElementById("novoProdutoBtn");
    const novoProdutoOverlay = document.getElementById("novoProdutoOverlay");
    const fecharNovoProdutoBtn = document.getElementById("fecharNovoProdutoBtn");
    const cancelarNovoProdutoBtn = document.getElementById("cancelarNovoProdutoBtn");
    const salvarNovoProdutoBtn = document.getElementById("salvarNovoProdutoBtn");
    const npVoltarEtapaBtn = document.getElementById("npVoltarEtapaBtn");
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
    const TOTAL_ETAPAS = 6;
    let etapaAtual = 1;

    const fecharMovimentacaoBtn = document.getElementById("fecharMovimentacaoBtn");
    const modalMovimentacao = document.getElementById("modalMovimentacao");
    const movProdutoSelect = document.getElementById("movProdutoSelect");
    const movQtdMenos = document.getElementById("movQtdMenos");
    const movQtdMais = document.getElementById("movQtdMais");
    const movQtdValor = document.getElementById("movQtdValor");
    const movMotivo = document.getElementById("movMotivo");
    const movSaldoInfo = document.getElementById("movSaldoInfo");
    const confirmarMovimentacaoBtn = document.getElementById("confirmarMovimentacaoBtn");

    const painelAcoes = document.getElementById("painelAcoes");
    const fecharPainelBtn = document.getElementById("fecharPainelBtn");
    const painelIncluirCadastro = document.getElementById("painelIncluirCadastro");
    const maisAcoesToggle = document.getElementById("maisAcoesToggle");
    const maisAcoesIcone = document.getElementById("maisAcoesIcone");
    const maisAcoesConteudo = document.getElementById("maisAcoesConteudo");

    let movQtd = 1;

    function formatBRL(valor) {
        return "R$ " + valor.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function statusDoProduto(item) {
        if (item.saldoAtual <= 0) return "esgotado";
        if (item.saldoAtual <= item.estoqueMinimo) return "baixo";
        return "ok";
    }

    function metodoTipoSelecionado() {
        const marcado = document.querySelector('input[name="movTipo"]:checked');
        return marcado ? marcado.value : "entrada";
    }

    function atualizarSelecaoTipo() {
        document.querySelectorAll(".mov-tipo-opcao").forEach(function (label) {
            const input = label.querySelector('input[type="radio"]');
            const ligado = input.checked;
            const entrada = label.dataset.tipo === "entrada";
            label.classList.toggle("border-secondary", ligado && entrada);
            label.classList.toggle("bg-secondary-container/10", ligado && entrada);
            label.classList.toggle("border-error", ligado && !entrada);
            label.classList.toggle("bg-error-container/10", ligado && !entrada);
        });
    }

    function renderResumo() {
        const valorEstoque = produtos.reduce(function (soma, p) { return soma + p.saldoAtual * p.custoUnitario; }, 0);
        const baixo = produtos.filter(function (p) { return statusDoProduto(p) === "baixo"; }).length;
        const esgotado = produtos.filter(function (p) { return statusDoProduto(p) === "esgotado"; }).length;

        resumoTotalProdutos.textContent = produtos.length;
        resumoValorEstoque.textContent = formatBRL(valorEstoque);
        resumoBaixo.textContent = baixo;
        resumoEsgotado.textContent = esgotado;
    }

    function produtosFiltrados() {
        const busca = filtroBusca.value.trim().toLowerCase();
        const status = filtroStatus.value;

        return produtos.filter(function (p) {
            const bateBusca = !busca || p.nome.toLowerCase().includes(busca) || p.codigo.toLowerCase().includes(busca);
            const bateStatus = !status || statusDoProduto(p) === status;
            return bateBusca && bateStatus;
        });
    }

    function renderChipsFiltro() {
        const partes = [];
        if (filtroStatus.value) partes.push("Status: " + filtroStatus.options[filtroStatus.selectedIndex].text);
        if (filtroBusca.value.trim()) partes.push('Busca: "' + filtroBusca.value.trim() + '"');

        chipsFiltro.classList.toggle("hidden", partes.length === 0);
        chipsFiltroTexto.textContent = partes.join(" · ");
    }

    function renderSelecionadosInfo() {
        selecionadosInfo.textContent = selecionados.size > 0 ? selecionados.size + " selecionado(s)" : "";
        excluirSelecionadosBtn.disabled = selecionados.size === 0;
        const lista = produtosFiltrados();
        selecionarTodosCheckbox.checked = lista.length > 0 && lista.every(function (p) { return selecionados.has(p.codigo); });
    }

    function precoCelulaHtml(item) {
        return '<div class="preco-cell inline-flex items-center gap-xs justify-end" data-codigo="' + item.codigo + '">' +
            '<button type="button" class="editar-preco-btn w-6 h-6 rounded-full hover:bg-surface-container-low inline-flex items-center justify-center text-on-surface-variant transition-colors" title="Editar preço">' +
                '<span class="material-symbols-outlined text-[16px]">edit</span>' +
            '</button>' +
            '<span class="preco-valor font-body-md text-body-md text-on-surface">' + formatBRL(item.precoVenda) + '</span>' +
        '</div>';
    }

    function renderTabela() {
        const lista = produtosFiltrados();
        estoqueBody.innerHTML = "";
        vazioState.classList.toggle("hidden", lista.length > 0);

        lista.forEach(function (item) {
            const marcado = selecionados.has(item.codigo);
            const tr = document.createElement("tr");
            tr.className = "border-t border-outline-variant row-in hover:bg-surface-container-low/60 transition-colors";
            tr.dataset.codigo = item.codigo;
            tr.innerHTML =
                '<td class="pl-md"><input type="checkbox" data-codigo="' + item.codigo + '" class="linha-checkbox w-4 h-4 accent-secondary" ' + (marcado ? "checked" : "") + ' /></td>' +
                '<td class="px-md py-sm font-body-md text-body-md text-on-surface">' + item.nome + '</td>' +
                '<td class="px-md py-sm font-body-md text-body-md text-on-surface-variant">' + item.codigo + '</td>' +
                '<td class="px-md py-sm preco-td text-right">' + precoCelulaHtml(item) + '</td>' +
                '<td class="px-md py-sm font-body-md text-body-md text-on-surface text-center">' + item.saldoAtual + '</td>' +
                '<td class="pr-md">' +
                    '<div class="flex items-center justify-center gap-xs">' +
                        '<button type="button" data-codigo="' + item.codigo + '" class="editar-produto-btn w-8 h-8 rounded-full hover:bg-surface-container-low inline-flex items-center justify-center text-on-surface-variant transition-colors" title="Editar produto">' +
                            '<span class="material-symbols-outlined text-[18px]">edit</span>' +
                        '</button>' +
                        '<button type="button" data-codigo="' + item.codigo + '" class="ajustar-btn w-8 h-8 rounded-full hover:bg-surface-container-low inline-flex items-center justify-center text-on-surface-variant transition-colors" title="Registrar movimentação">' +
                            '<span class="material-symbols-outlined text-[18px]">sync_alt</span>' +
                        '</button>' +
                        '<div class="relative">' +
                            '<button type="button" data-codigo="' + item.codigo + '" class="menu-btn w-8 h-8 rounded-full hover:bg-surface-container-low inline-flex items-center justify-center text-on-surface-variant transition-colors" title="Mais opções">' +
                                '<span class="material-symbols-outlined text-[18px]">more_vert</span>' +
                            '</button>' +
                            '<div class="row-menu hidden absolute right-0 top-full mt-xs bg-white border border-outline-variant rounded-lg shadow-lg py-xs w-44 z-10">' +
                                '<button type="button" data-codigo="' + item.codigo + '" class="excluir-produto-btn w-full text-left px-md py-xs font-body-md text-body-md text-on-surface hover:bg-surface-container-low transition-colors">Excluir produto</button>' +
                            '</div>' +
                        '</div>' +
                    '</div>' +
                '</td>';
            estoqueBody.appendChild(tr);
        });

        renderResumo();
        renderChipsFiltro();
        renderSelecionadosInfo();
    }

    function mostrarToast(mensagem, erro) {
        toastMsg.textContent = mensagem;
        toast.classList.remove("hidden", "bg-secondary-container", "text-on-secondary-container", "bg-error-container", "text-error");
        toast.classList.add.apply(toast.classList, erro ? ["bg-error-container", "text-error"] : ["bg-secondary-container", "text-on-secondary-container"]);
        toastIcon.textContent = erro ? "error" : "check_circle";
        window.clearTimeout(mostrarToast._timer);
        mostrarToast._timer = window.setTimeout(function () {
            toast.classList.add("hidden");
        }, 3500);
    }

    function popularSelectProdutos() {
        movProdutoSelect.innerHTML = "";
        produtos.forEach(function (p) {
            const opt = document.createElement("option");
            opt.value = p.codigo;
            opt.textContent = p.nome + " (saldo atual: " + p.saldoAtual + ")";
            movProdutoSelect.appendChild(opt);
        });
    }

    function atualizarSaldoInfo() {
        const produto = produtos.find(function (p) { return p.codigo === movProdutoSelect.value; });
        if (!produto) return;
        const tipo = metodoTipoSelecionado();
        const resultado = tipo === "entrada" ? produto.saldoAtual + movQtd : produto.saldoAtual - movQtd;
        movSaldoInfo.textContent = "Saldo atual: " + produto.saldoAtual + " → saldo após confirmar: " + Math.max(resultado, 0) + (resultado < 0 ? " (quantidade maior que o saldo disponível)" : "");
        movSaldoInfo.classList.toggle("text-error", resultado < 0);
        movSaldoInfo.classList.toggle("text-on-surface-variant", resultado >= 0);
    }

    function abrirModal() {
        popularSelectProdutos();
        movQtd = 1;
        movQtdValor.textContent = movQtd;
        movMotivo.value = "";
        document.querySelector('input[name="movTipo"][value="entrada"]').checked = true;
        atualizarSelecaoTipo();
        atualizarSaldoInfo();
        modalMovimentacao.classList.remove("hidden");
    }

    function fecharModal() {
        modalMovimentacao.classList.add("hidden");
    }

    function irParaEtapa(n) {
        etapaAtual = Math.min(Math.max(n, 1), TOTAL_ETAPAS);
        npSteps.forEach(function (step) {
            const ativo = parseInt(step.dataset.step, 10) === etapaAtual;
            const num = step.querySelector(".np-step-num");
            const label = step.querySelector(".np-step-label");
            step.classList.toggle("bg-secondary-container/40", ativo);
            num.className = "np-step-num w-6 h-6 rounded-full flex items-center justify-center font-label-sm text-label-sm font-bold shrink-0 " +
                (ativo ? "bg-secondary text-white" : "border border-outline-variant text-outline");
            label.classList.toggle("text-secondary", ativo);
            label.classList.toggle("font-semibold", ativo);
            label.classList.toggle("text-on-surface-variant", !ativo);
        });
        npPanels.forEach(function (panel) {
            panel.classList.toggle("hidden", parseInt(panel.dataset.stepPanel, 10) !== etapaAtual);
        });
        npVoltarEtapaBtn.classList.toggle("hidden", etapaAtual === 1);
        npAvancarEtapaBtn.classList.toggle("hidden", etapaAtual === TOTAL_ETAPAS);
    }

    function abrirNovoProduto() {
        novoProdutoOverlay.querySelectorAll("input, textarea").forEach(function (campo) { campo.value = ""; });
        novoProdutoOverlay.querySelectorAll("select").forEach(function (campo) { campo.selectedIndex = 0; });
        irParaEtapa(1);
        novoProdutoOverlay.classList.remove("hidden");
        novoProdutoOverlay.scrollTop = 0;
        npNome.focus();
    }

    function fecharNovoProduto() {
        novoProdutoOverlay.classList.add("hidden");
    }

    function parseDecimal(valor) {
        return parseFloat(String(valor).replace(/\./g, "").replace(",", ".")) || 0;
    }

    function salvarNovoProduto() {
        const nome = npNome.value.trim();
        if (!nome) {
            irParaEtapa(1);
            npNome.focus();
            mostrarToast("Informe o nome do produto para continuar.", true);
            return;
        }

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

        renderTabela();
        fecharNovoProduto();
        mostrarToast("Produto \"" + nome + "\" cadastrado (exemplo, ainda não gravado no banco de dados).");
    }

    novoProdutoBtn.addEventListener("click", abrirNovoProduto);
    fecharNovoProdutoBtn.addEventListener("click", fecharNovoProduto);
    cancelarNovoProdutoBtn.addEventListener("click", fecharNovoProduto);
    salvarNovoProdutoBtn.addEventListener("click", salvarNovoProduto);
    npVoltarEtapaBtn.addEventListener("click", function () { irParaEtapa(etapaAtual - 1); });
    npAvancarEtapaBtn.addEventListener("click", function () { irParaEtapa(etapaAtual + 1); });
    npSteps.forEach(function (step) {
        step.addEventListener("click", function () { irParaEtapa(parseInt(step.dataset.step, 10)); });
    });

    fecharMovimentacaoBtn.addEventListener("click", fecharModal);
    modalMovimentacao.addEventListener("click", function (e) {
        if (e.target === modalMovimentacao) fecharModal();
    });

    function fecharMenus() {
        document.querySelectorAll(".row-menu").forEach(function (menu) { menu.classList.add("hidden"); });
    }

    function salvarPreco(codigo, valorDigitado) {
        const item = produtos.find(function (p) { return p.codigo === codigo; });
        if (!item) return;
        let novoPreco = parseFloat(String(valorDigitado).replace(",", "."));
        if (isNaN(novoPreco) || novoPreco < 0) novoPreco = item.precoVenda;
        const mudou = novoPreco !== item.precoVenda;
        item.precoVenda = novoPreco;
        const celula = estoqueBody.querySelector('.preco-cell[data-codigo="' + codigo + '"]');
        if (celula) celula.outerHTML = precoCelulaHtml(item);
        if (mudou) mostrarToast("Preço de \"" + item.nome + "\" atualizado para " + formatBRL(novoPreco) + " (exemplo, ainda não gravado no banco de dados).");
    }

    estoqueBody.addEventListener("click", function (e) {
        const ajustarBtn = e.target.closest(".ajustar-btn");
        if (ajustarBtn) {
            abrirModal();
            movProdutoSelect.value = ajustarBtn.dataset.codigo;
            atualizarSaldoInfo();
            return;
        }

        const editarPrecoBtn = e.target.closest(".editar-preco-btn");
        if (editarPrecoBtn) {
            const codigo = editarPrecoBtn.closest(".preco-cell").dataset.codigo;
            const item = produtos.find(function (p) { return p.codigo === codigo; });
            if (!item) return;
            const celula = editarPrecoBtn.closest(".preco-cell");
            celula.innerHTML =
                '<span class="font-body-md text-body-md text-on-surface-variant">R$</span>' +
                '<input type="number" min="0" step="0.01" value="' + item.precoVenda.toFixed(2) + '" class="preco-input w-20 text-right bg-surface-container-low border-none rounded-md py-1 px-xs font-body-md text-body-md" />';
            const input = celula.querySelector(".preco-input");
            input.focus();
            input.select();
            return;
        }

        const editarProdutoBtn = e.target.closest(".editar-produto-btn");
        if (editarProdutoBtn) {
            fecharMenus();
            mostrarToast("Edição completa do produto ainda não está disponível nesta versão.", true);
            return;
        }

        const menuBtn = e.target.closest(".menu-btn");
        if (menuBtn) {
            const menu = menuBtn.parentElement.querySelector(".row-menu");
            const jaAberto = !menu.classList.contains("hidden");
            fecharMenus();
            menu.classList.toggle("hidden", jaAberto);
            return;
        }

        const excluirProdutoBtn = e.target.closest(".excluir-produto-btn");
        if (excluirProdutoBtn) {
            const codigo = excluirProdutoBtn.dataset.codigo;
            const item = produtos.find(function (p) { return p.codigo === codigo; });
            if (!item) return;
            if (!window.confirm("Excluir \"" + item.nome + "\" da listagem?")) return;
            const indice = produtos.findIndex(function (p) { return p.codigo === codigo; });
            if (indice >= 0) produtos.splice(indice, 1);
            selecionados.delete(codigo);
            renderTabela();
            mostrarToast("Produto removido da listagem (exemplo, ainda não gravado no banco de dados).");
            return;
        }

        if (!e.target.closest(".row-menu")) fecharMenus();
    });

    estoqueBody.addEventListener("keydown", function (e) {
        if (!e.target.matches(".preco-input")) return;
        if (e.key === "Enter") {
            e.preventDefault();
            const codigo = e.target.closest(".preco-cell").dataset.codigo;
            salvarPreco(codigo, e.target.value);
        } else if (e.key === "Escape") {
            const item = produtos.find(function (p) { return p.codigo === e.target.closest(".preco-cell").dataset.codigo; });
            const celula = e.target.closest(".preco-cell");
            if (item && celula) celula.outerHTML = precoCelulaHtml(item);
        }
    });

    estoqueBody.addEventListener("focusout", function (e) {
        if (!e.target.matches(".preco-input")) return;
        const celula = e.target.closest(".preco-cell");
        if (!celula) return;
        salvarPreco(celula.dataset.codigo, e.target.value);
    });

    estoqueBody.addEventListener("change", function (e) {
        if (!e.target.matches(".linha-checkbox")) return;
        const codigo = e.target.dataset.codigo;
        if (e.target.checked) selecionados.add(codigo); else selecionados.delete(codigo);
        renderSelecionadosInfo();
    });

    selecionarTodosCheckbox.addEventListener("change", function () {
        const lista = produtosFiltrados();
        if (selecionarTodosCheckbox.checked) {
            lista.forEach(function (p) { selecionados.add(p.codigo); });
        } else {
            lista.forEach(function (p) { selecionados.delete(p.codigo); });
        }
        renderTabela();
    });

    excluirSelecionadosBtn.addEventListener("click", function () {
        if (selecionados.size === 0) return;
        if (!window.confirm("Remover " + selecionados.size + " produto(s) selecionado(s) da listagem?")) return;
        for (let i = produtos.length - 1; i >= 0; i--) {
            if (selecionados.has(produtos[i].codigo)) produtos.splice(i, 1);
        }
        selecionados.clear();
        renderTabela();
        mostrarToast("Produtos removidos da listagem (exemplo, ainda não gravado no banco de dados).");
    });

    movProdutoSelect.addEventListener("change", atualizarSaldoInfo);

    document.querySelectorAll('input[name="movTipo"]').forEach(function (input) {
        input.addEventListener("change", function () {
            atualizarSelecaoTipo();
            atualizarSaldoInfo();
        });
    });

    movQtdMenos.addEventListener("click", function () {
        if (movQtd > 1) movQtd -= 1;
        movQtdValor.textContent = movQtd;
        atualizarSaldoInfo();
    });

    movQtdMais.addEventListener("click", function () {
        movQtd += 1;
        movQtdValor.textContent = movQtd;
        atualizarSaldoInfo();
    });

    confirmarMovimentacaoBtn.addEventListener("click", function () {
        const produto = produtos.find(function (p) { return p.codigo === movProdutoSelect.value; });
        if (!produto) return;
        const tipo = metodoTipoSelecionado();

        if (tipo === "saida" && movQtd > produto.saldoAtual) {
            mostrarToast("Não é possível registrar saída de " + movQtd + " unidades: o saldo atual de \"" + produto.nome + "\" é " + produto.saldoAtual + ".", true);
            return;
        }

        produto.saldoAtual = tipo === "entrada" ? produto.saldoAtual + movQtd : produto.saldoAtual - movQtd;
        renderTabela();
        fecharModal();
        mostrarToast(
            (tipo === "entrada" ? "Entrada de " : "Saída de ") + movQtd + " unidade(s) registrada em \"" + produto.nome + "\" (exemplo, ainda não gravado no banco de dados)."
        );
    });

    toggleFiltrosBtn.addEventListener("click", function () {
        filtrosExtras.classList.toggle("hidden");
    });

    limparFiltrosBtn.addEventListener("click", function () {
        filtroBusca.value = "";
        filtroStatus.value = "";
        renderTabela();
    });

    atualizarListaBtn.addEventListener("click", function () {
        renderTabela();
        mostrarToast("Lista atualizada.");
    });

    imprimirBtn.addEventListener("click", function () {
        window.print();
    });

    filtroBusca.addEventListener("input", renderTabela);
    filtroStatus.addEventListener("change", renderTabela);

    // Painel de ações lateral (trilho estilo Bling)
    function exportarCsv() {
        const linhas = [["Código", "Descrição", "Categoria", "Saldo", "Mínimo", "Custo Unitário", "Preço de Venda"]];
        produtos.forEach(function (p) {
            linhas.push([p.codigo, p.nome, p.categoria, p.saldoAtual, p.estoqueMinimo, p.custoUnitario.toFixed(2), p.precoVenda.toFixed(2)]);
        });
        const csv = linhas.map(function (linha) {
            return linha.map(function (campo) { return '"' + String(campo).replace(/"/g, '""') + '"'; }).join(";");
        }).join("\r\n");
        const blob = new Blob(["﻿" + csv], { type: "text/csv;charset=utf-8;" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = "estoque.csv";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }

    function executarAcaoPainel(acao) {
        switch (acao) {
            case "movimentacao":
                abrirModal();
                break;
            case "exportar":
            case "exportarDados":
                exportarCsv();
                mostrarToast("Planilha exportada.");
                break;
            case "etiquetas":
                if (selecionados.size === 0) {
                    mostrarToast("Selecione ao menos um produto para imprimir etiquetas.", true);
                } else {
                    mostrarToast("Impressão de etiquetas ainda não disponível nesta versão.", true);
                }
                break;
            default:
                mostrarToast("Essa ação ainda não está disponível nesta versão.", true);
        }
    }

    document.querySelectorAll(".painel-acao").forEach(function (btn) {
        btn.addEventListener("click", function () {
            executarAcaoPainel(btn.dataset.acao);
        });
    });

    document.querySelectorAll(".rail-btn").forEach(function (btn) {
        btn.addEventListener("click", function () {
            painelAcoes.classList.remove("hidden");
        });
    });

    fecharPainelBtn.addEventListener("click", function () {
        painelAcoes.classList.add("hidden");
    });

    painelIncluirCadastro.addEventListener("click", function () {
        painelAcoes.classList.add("hidden");
        abrirNovoProduto();
    });

    maisAcoesToggle.addEventListener("click", function () {
        const aberto = !maisAcoesConteudo.classList.contains("hidden");
        maisAcoesConteudo.classList.toggle("hidden");
        maisAcoesIcone.textContent = aberto ? "add" : "remove";
    });

    document.addEventListener("click", function (e) {
        if (!e.target.closest(".menu-btn") && !e.target.closest(".row-menu")) fecharMenus();
    });

    renderTabela();
});
