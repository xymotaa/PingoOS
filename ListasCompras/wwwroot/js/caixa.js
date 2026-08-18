document.addEventListener("DOMContentLoaded", function () {
    const cart = [];

    const buscaForm = document.getElementById("buscaForm");
    const buscaInput = document.getElementById("buscaInput");
    const itensBody = document.getElementById("itensBody");
    const vazioState = document.getElementById("vazioState");
    const subtotalLabel = document.getElementById("subtotalLabel");
    const subtotalValor = document.getElementById("subtotalValor");
    const descontoValor = document.getElementById("descontoValor");
    const totalValor = document.getElementById("totalValor");
    const trocoValor = document.getElementById("trocoValor");
    const valorRecebido = document.getElementById("valorRecebido");
    const painelDinheiro = document.getElementById("painelDinheiro");
    const finalizarBtn = document.getElementById("finalizarBtn");
    const toast = document.getElementById("toast");
    const toastMsg = document.getElementById("toastMsg");
    const toastIcon = document.getElementById("toastIcon");

    // O banner de TempData vem do servidor já renderizado (não passa por mostrarToast);
    // some sozinho como o toast, senão fica preso na tela até a próxima navegação —
    // e na frente de caixa a pessoa continua vendendo sem recarregar a página.
    ["avisoSucesso", "avisoAtencao", "avisoErro"].forEach(function (id) {
        var el = document.getElementById(id);
        if (!el) return;
        window.setTimeout(function () {
            el.style.transition = "opacity .3s ease";
            el.style.opacity = "0";
            window.setTimeout(function () { el.remove(); }, 300);
        }, 3000);
    });

    function formatBRL(valor) {
        return "R$ " + valor.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function metodoSelecionado() {
        const marcado = document.querySelector('input[name="metodo"]:checked');
        return marcado ? marcado.value : "dinheiro";
    }

    function atualizarSelecaoPagamento() {
        document.querySelectorAll(".pagamento-opcao").forEach(function (label) {
            const input = label.querySelector('input[type="radio"]');
            label.classList.toggle("border-secondary", input.checked);
            label.classList.toggle("bg-secondary-container/10", input.checked);
        });
        painelDinheiro.classList.toggle("hidden", metodoSelecionado() !== "dinheiro");
    }

    function buscarProduto(termo) {
        const alvo = termo.trim().toLowerCase();
        if (!alvo) return null;
        return PRODUTOS.find(function (p) {
            return p.codigo.toLowerCase() === alvo || p.nome.toLowerCase().includes(alvo);
        }) || null;
    }

    function buscarProdutos(termo) {
        const alvo = termo.trim().toLowerCase();
        if (!alvo) return [];
        return PRODUTOS.filter(function (p) {
            return p.codigo.toLowerCase().includes(alvo) || p.nome.toLowerCase().includes(alvo);
        }).slice(0, 8);
    }

    // Preço final da unidade após desconto. O tipo (percentual ou valor em
    // reais) só muda a fórmula usada, o resultado final é sempre o preço
    // unitário líquido, e desconto 0 nunca altera o preço cheio.
    function precoComDesconto(item) {
        if (!item.desconto) return item.precoUnitario;
        if (item.descontoTipo === "valor") {
            return Math.max(item.precoUnitario - item.desconto, 0);
        }
        return item.precoUnitario * (1 - Math.min(item.desconto, 100) / 100);
    }

    function subtotalItem(item) {
        return item.qtd * precoComDesconto(item);
    }

    function descontoItemTotal(item) {
        return item.qtd * (item.precoUnitario - precoComDesconto(item));
    }

    function descontoCelulaHtml(item, index) {
        const max = item.descontoTipo === "valor" ? item.precoUnitario : 100;
        const step = item.descontoTipo === "valor" ? "0.01" : "1";
        return '<div class="inline-flex items-center bg-surface-container-low rounded-md overflow-hidden">' +
            '<input type="number" min="0" max="' + max + '" step="' + step + '" value="' + item.desconto + '" data-index="' + index + '" ' +
            'class="desconto-input w-14 text-center bg-transparent border-none py-1 font-body-md text-body-md [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none" />' +
            '<button type="button" data-index="' + index + '" title="Alternar entre % e R$" ' +
            'class="desconto-toggle w-6 h-6 shrink-0 rounded-full hover:bg-white flex items-center justify-center font-label-sm text-label-sm font-bold text-on-surface-variant leading-none transition-colors">' +
            (item.descontoTipo === "valor" ? "R$" : "%") +
            '</button>' +
        '</div>';
    }

    function adicionarItem(produto) {
        const existente = cart.find(function (i) { return i.codigo === produto.codigo; });
        if (existente) {
            existente.qtd += 1;
        } else {
            cart.push({ id: produto.id, codigo: produto.codigo, nome: produto.nome, precoUnitario: produto.precoUnitario, qtd: 1, desconto: 0, descontoTipo: "percentual" });
        }
        renderTabela();
    }

    function renderTabela() {
        itensBody.innerHTML = "";
        vazioState.classList.toggle("hidden", cart.length > 0);

        cart.forEach(function (item, index) {
            const tr = document.createElement("tr");
            tr.className = "border-t border-outline-variant row-in";
            tr.dataset.index = String(index);
            tr.innerHTML =
                '<td class="px-md py-sm font-body-md text-body-md text-on-surface-variant">' + String(index + 1).padStart(3, "0") + '</td>' +
                '<td class="px-md py-sm font-body-md text-body-md text-on-surface-variant">' + item.codigo + '</td>' +
                '<td class="px-md py-sm font-body-md text-body-md text-on-surface font-semibold">' + item.nome + '</td>' +
                '<td class="px-md py-sm text-center">' +
                    '<div class="inline-flex items-center gap-xs bg-surface-container-low rounded-md px-xs py-xs">' +
                        '<button type="button" data-index="' + index + '" class="qtd-btn qtd-dec w-6 h-6 rounded-full hover:bg-white flex items-center justify-center text-on-surface-variant font-bold leading-none transition-colors">−</button>' +
                        '<span class="qtd-valor w-5 text-center font-label-md text-label-md font-bold text-primary">' + item.qtd + '</span>' +
                        '<button type="button" data-index="' + index + '" class="qtd-btn qtd-inc w-6 h-6 rounded-full hover:bg-white flex items-center justify-center text-on-surface-variant font-bold leading-none transition-colors">+</button>' +
                    '</div>' +
                '</td>' +
                '<td class="px-md py-sm font-body-md text-body-md text-right">' + formatBRL(item.precoUnitario) + '</td>' +
                '<td class="px-md py-sm text-center desconto-cell">' + descontoCelulaHtml(item, index) + '</td>' +
                '<td class="px-md py-sm font-body-md text-body-md font-semibold text-right subtotal-cell">' + formatBRL(subtotalItem(item)) + '</td>' +
                '<td class="px-md py-sm text-center">' +
                    '<button type="button" data-index="' + index + '" class="remove-btn w-8 h-8 rounded-full hover:bg-error-container text-error inline-flex items-center justify-center transition-colors">' +
                        '<span class="material-symbols-outlined text-[18px]">delete</span>' +
                    '</button>' +
                '</td>';
            itensBody.appendChild(tr);
        });

        recalcular();
    }

    function recalcular() {
        let subtotalBruto = 0;
        let totalDesconto = 0;
        let totalItens = 0;
        cart.forEach(function (item) {
            subtotalBruto += item.qtd * item.precoUnitario;
            totalDesconto += descontoItemTotal(item);
            totalItens += item.qtd;
        });
        const total = subtotalBruto - totalDesconto;

        subtotalLabel.textContent = "Subtotal (" + totalItens + (totalItens === 1 ? " item)" : " itens)");
        subtotalValor.textContent = formatBRL(subtotalBruto);
        descontoValor.textContent = "- " + formatBRL(totalDesconto);
        totalValor.textContent = formatBRL(total);

        const recebido = parseFloat(valorRecebido.value) || 0;
        const troco = recebido - total;
        trocoValor.textContent = formatBRL(Math.max(troco, 0));
        trocoValor.classList.toggle("text-error", troco < 0 && metodoSelecionado() === "dinheiro");
        trocoValor.classList.toggle("text-primary", !(troco < 0 && metodoSelecionado() === "dinheiro"));

        const precisaDinheiro = metodoSelecionado() === "dinheiro" && total > 0;
        const podeFinalizar = cart.length > 0 && (!precisaDinheiro || recebido >= total);

        finalizarBtn.disabled = !podeFinalizar;
        finalizarBtn.classList.toggle("bg-primary", podeFinalizar);
        finalizarBtn.classList.toggle("text-white", podeFinalizar);
        finalizarBtn.classList.toggle("hover:bg-opacity-90", podeFinalizar);
        finalizarBtn.classList.toggle("cursor-pointer", podeFinalizar);
        finalizarBtn.classList.toggle("bg-outline-variant", !podeFinalizar);
        finalizarBtn.classList.toggle("text-outline", !podeFinalizar);
        finalizarBtn.classList.toggle("cursor-not-allowed", !podeFinalizar);
    }

    function mostrarToast(mensagem, erro) {
        toastMsg.textContent = mensagem;
        toast.classList.remove("hidden", "bg-secondary-container", "text-on-secondary-container", "bg-error-container", "text-error");
        toast.classList.add.apply(toast.classList, erro ? ["bg-error-container", "text-error"] : ["bg-secondary-container", "text-on-secondary-container"]);
        toastIcon.textContent = erro ? "error" : "check_circle";
        window.clearTimeout(mostrarToast._timer);
        mostrarToast._timer = window.setTimeout(function () {
            toast.classList.add("hidden");
        }, 3000);
    }

    buscaForm.addEventListener("submit", function (e) {
        e.preventDefault();
        const produto = buscarProduto(buscaInput.value);
        if (!produto) {
            buscaInput.classList.add("ring-2", "ring-error");
            window.setTimeout(function () { buscaInput.classList.remove("ring-2", "ring-error"); }, 1200);
            return;
        }
        adicionarItem(produto);
        buscaInput.value = "";
        buscaInput.focus();
        fecharSugestoes();
    });

    // ===== Sugestões de produto: dropdown do mesmo tamanho do campo de busca =====

    const sugestoesProduto = document.getElementById("sugestoesProduto");
    let indiceSugestaoAtiva = -1;

    function fecharSugestoes() {
        sugestoesProduto.classList.add("hidden");
        sugestoesProduto.innerHTML = "";
        indiceSugestaoAtiva = -1;
    }

    function selecionarProdutoDaLista(produto) {
        adicionarItem(produto);
        buscaInput.value = "";
        buscaInput.focus();
        fecharSugestoes();
    }

    function renderSugestoes() {
        const encontrados = buscarProdutos(buscaInput.value);
        if (encontrados.length === 0) {
            fecharSugestoes();
            return;
        }

        sugestoesProduto.innerHTML = "";
        encontrados.forEach(function (p, i) {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "sugestao-item w-full text-left px-md py-sm border-b border-outline-variant last:border-0 hover:bg-surface-container-low transition-colors flex items-center justify-between gap-md" +
                (i === indiceSugestaoAtiva ? " bg-surface-container-low" : "");

            const esquerda = document.createElement("div");
            esquerda.className = "min-w-0";
            const nome = document.createElement("p");
            nome.className = "font-body-md text-body-md text-on-surface truncate";
            nome.textContent = p.nome;
            const detalhe = document.createElement("p");
            detalhe.className = "font-label-sm text-label-sm text-outline";
            detalhe.textContent = p.codigo + " · estoque: " + p.saldoAtual;
            esquerda.append(nome, detalhe);

            const preco = document.createElement("span");
            preco.className = "font-body-md text-body-md font-semibold text-secondary shrink-0";
            preco.textContent = formatBRL(p.precoUnitario);

            item.append(esquerda, preco);
            item.addEventListener("click", function () { selecionarProdutoDaLista(p); });
            sugestoesProduto.appendChild(item);
        });

        sugestoesProduto.classList.remove("hidden");
    }

    buscaInput.addEventListener("input", function () {
        indiceSugestaoAtiva = -1;
        renderSugestoes();
    });

    buscaInput.addEventListener("keydown", function (e) {
        const itens = sugestoesProduto.querySelectorAll(".sugestao-item");
        if (itens.length === 0) return;

        if (e.key === "ArrowDown") {
            e.preventDefault();
            indiceSugestaoAtiva = Math.min(indiceSugestaoAtiva + 1, itens.length - 1);
            renderSugestoes();
        } else if (e.key === "ArrowUp") {
            e.preventDefault();
            indiceSugestaoAtiva = Math.max(indiceSugestaoAtiva - 1, 0);
            renderSugestoes();
        } else if (e.key === "Enter" && indiceSugestaoAtiva >= 0) {
            e.preventDefault();
            const encontrados = buscarProdutos(buscaInput.value);
            if (encontrados[indiceSugestaoAtiva]) selecionarProdutoDaLista(encontrados[indiceSugestaoAtiva]);
        } else if (e.key === "Escape") {
            fecharSugestoes();
        }
    });

    document.addEventListener("click", function (e) {
        if (!buscaForm.contains(e.target)) fecharSugestoes();
    });

    itensBody.addEventListener("click", function (e) {
        const removeBtn = e.target.closest(".remove-btn");
        if (removeBtn) {
            const index = parseInt(removeBtn.dataset.index, 10);
            cart.splice(index, 1);
            renderTabela();
            return;
        }

        const qtdBtn = e.target.closest(".qtd-btn");
        if (qtdBtn) {
            const index = parseInt(qtdBtn.dataset.index, 10);
            if (qtdBtn.classList.contains("qtd-inc")) {
                cart[index].qtd += 1;
            } else if (cart[index].qtd > 1) {
                cart[index].qtd -= 1;
            }
            const linha = itensBody.querySelector('tr[data-index="' + index + '"]');
            if (linha) {
                linha.querySelector(".qtd-valor").textContent = cart[index].qtd;
                linha.querySelector(".subtotal-cell").textContent = formatBRL(subtotalItem(cart[index]));
            }
            recalcular();
            return;
        }

        const toggleBtn = e.target.closest(".desconto-toggle");
        if (toggleBtn) {
            const index = parseInt(toggleBtn.dataset.index, 10);
            const item = cart[index];
            item.descontoTipo = item.descontoTipo === "valor" ? "percentual" : "valor";
            item.desconto = 0;
            const linha = itensBody.querySelector('tr[data-index="' + index + '"]');
            if (linha) {
                linha.querySelector(".desconto-cell").innerHTML = descontoCelulaHtml(item, index);
                linha.querySelector(".subtotal-cell").textContent = formatBRL(subtotalItem(item));
            }
            recalcular();
        }
    });

    itensBody.addEventListener("input", function (e) {
        if (!e.target.matches(".desconto-input")) return;
        const index = parseInt(e.target.dataset.index, 10);
        const item = cart[index];
        const max = item.descontoTipo === "valor" ? item.precoUnitario : 100;
        let valor = parseFloat(e.target.value);
        if (isNaN(valor) || valor < 0) valor = 0;
        if (valor > max) {
            valor = max;
            e.target.value = max;
            e.target.classList.add("ring-2", "ring-error");
            window.setTimeout(function () { e.target.classList.remove("ring-2", "ring-error"); }, 1200);
            mostrarToast(
                item.descontoTipo === "valor"
                    ? "O desconto de \"" + item.nome + "\" não pode ultrapassar o valor do produto (" + formatBRL(item.precoUnitario) + ")."
                    : "O desconto de \"" + item.nome + "\" não pode ultrapassar 100%.",
                true
            );
        }
        item.desconto = valor;
        const linha = itensBody.querySelector('tr[data-index="' + index + '"] .subtotal-cell');
        if (linha) linha.textContent = formatBRL(subtotalItem(item));
        recalcular();
    });

    document.querySelectorAll('input[name="metodo"]').forEach(function (input) {
        input.addEventListener("change", function () {
            atualizarSelecaoPagamento();
            recalcular();
        });
    });

    valorRecebido.addEventListener("input", recalcular);

    document.addEventListener("keydown", function (e) {
        if (e.key === "F2") {
            e.preventDefault();
            buscaInput.focus();
        }
    });

    atualizarSelecaoPagamento();
    renderTabela();

    // ===== Envio da venda =====

    const itensPost = document.getElementById("itensPost");
    const formaPagamentoPost = document.getElementById("formaPagamentoPost");
    const valorRecebidoPost = document.getElementById("valorRecebidoPost");

    if (finalizarBtn && finalizarBtn.form) {
        finalizarBtn.form.addEventListener("submit", function (e) {
            if (cart.length === 0) { e.preventDefault(); return; }

            // Decimais sempre com ponto: a cultura do sistema converteria "620,00" errado
            itensPost.innerHTML = "";
            cart.forEach(function (item) {
                function oculto(nome, valor) {
                    const i = document.createElement("input");
                    i.type = "hidden"; i.name = nome; i.value = valor;
                    itensPost.appendChild(i);
                }
                // O desconto pode ter sido digitado em reais; o servidor guarda sempre percentual
                let percentual = item.desconto || 0;
                if (item.descontoTipo === "valor" && item.precoUnitario > 0) {
                    percentual = Math.min(item.desconto / item.precoUnitario * 100, 100);
                }
                oculto("itemProdutoId", item.id);
                oculto("itemQuantidade", item.qtd);
                oculto("itemPreco", Number(item.precoUnitario).toFixed(2));
                oculto("itemDesconto", Number(percentual).toFixed(2));
            });

            formaPagamentoPost.value = metodoSelecionado();
            const recebido = parseFloat(valorRecebido.value) || 0;
            valorRecebidoPost.value = Number(recebido).toFixed(2);
        });
    }
});
