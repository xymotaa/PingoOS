document.addEventListener("DOMContentLoaded", function () {
    // Tela de edição de venda (ver CaixaController.EditarVenda): o carrinho nasce
    // preenchido em vez de vazio; nas demais telas essa constante não existe.
    const cart = (typeof ITENS_EM_EDICAO !== "undefined" ? ITENS_EM_EDICAO : []).map(function (i) {
        return { id: i.id, codigo: i.codigo, nome: i.nome, precoUnitario: i.precoUnitario, qtd: i.qtd, desconto: i.desconto, descontoTipo: i.descontoTipo, comentario: i.comentario || "" };
    });

    const buscaForm = document.getElementById("buscaForm");
    const buscaInput = document.getElementById("buscaInput");
    const itensBody = document.getElementById("itensBody");
    const vazioState = document.getElementById("vazioState");
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
    ["avisoErro"].forEach(function (id) {
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

    function parseDecimal(v) {
        const n = parseFloat(v);
        return isNaN(n) ? 0 : n;
    }

    function porId(id) {
        return PRODUTOS.find(function (p) { return p.id === id; }) || null;
    }

    // Além do que bate por texto, inclui produtos compatíveis (mesmo modelo de
    // celular, ver ProdutoEstoqueModeloCompativel) com o que foi encontrado — mesmo
    // que o compatível não tenha nada a ver com o texto digitado. Marcado com
    // _compativelDe pra renderResultadosProduto saber que precisa da etiqueta.
    function buscarProdutos(termo) {
        const alvo = termo.trim().toLowerCase();
        if (!alvo) return [];

        const encontrados = PRODUTOS.filter(function (p) {
            return p.codigo.toLowerCase().includes(alvo) || p.nome.toLowerCase().includes(alvo);
        });

        const resultado = encontrados.slice();
        const idsPresentes = new Set(encontrados.map(function (p) { return p.id; }));

        encontrados.forEach(function (p) {
            (p.compativeisComIds || []).forEach(function (idCompativel) {
                if (idsPresentes.has(idCompativel)) return;
                const compativel = porId(idCompativel);
                if (!compativel) return;
                idsPresentes.add(idCompativel);
                resultado.push(Object.assign({}, compativel, { _compativelDe: p.nome }));
            });
        });

        return resultado.slice(0, 20);
    }

    // Preço final da unidade após desconto percentual.
    function precoComDesconto(item) {
        if (!item.desconto) return item.precoUnitario;
        return item.precoUnitario * (1 - Math.min(item.desconto, 100) / 100);
    }

    function subtotalItem(item) {
        return item.qtd * precoComDesconto(item);
    }

    function descontoItemTotal(item) {
        return item.qtd * (item.precoUnitario - precoComDesconto(item));
    }

    function miniaturaHtml(imagemUrl, tamanho) {
        if (imagemUrl) {
            return '<img src="' + imagemUrl + '" alt="" class="w-full h-full object-cover" />';
        }
        return '<span class="material-symbols-outlined text-[' + (tamanho || 18) + 'px] text-outline">inventory_2</span>';
    }

    function renderTabela() {
        itensBody.innerHTML = "";
        vazioState.classList.toggle("hidden", cart.length > 0);

        cart.forEach(function (item, index) {
            const produto = porId(item.id);
            const tr = document.createElement("tr");
            tr.className = "border-t border-outline-variant row-in";
            tr.dataset.index = String(index);
            tr.innerHTML =
                '<td class="pl-lg py-sm w-12">' +
                    '<div class="w-10 h-10 rounded-lg bg-surface-container-low flex items-center justify-center overflow-hidden">' + miniaturaHtml(produto && produto.imagemUrl) + '</div>' +
                '</td>' +
                '<td class="pr-md py-sm">' +
                    '<p class="font-body-md text-body-md text-on-surface font-semibold">' + item.nome + '</p>' +
                    '<p class="font-label-sm text-label-sm text-outline">' + item.codigo + (item.comentario ? " · " + item.comentario : "") + '</p>' +
                '</td>' +
                '<td class="px-md py-sm text-center font-body-md text-body-md">' + item.qtd + '</td>' +
                '<td class="px-md py-sm font-body-md text-body-md text-right">' + formatBRL(item.precoUnitario) + (item.desconto ? '<span class="block font-label-sm text-label-sm text-error">-' + item.desconto + '%</span>' : '') + '</td>' +
                '<td class="px-md py-sm font-body-md text-body-md font-semibold text-right">' + formatBRL(subtotalItem(item)) + '</td>' +
                '<td class="pr-lg py-sm text-center">' +
                    '<div class="flex items-center justify-center gap-xs">' +
                        '<button type="button" data-index="' + index + '" class="editar-item-btn w-8 h-8 rounded-full hover:bg-surface-container-low text-on-surface-variant inline-flex items-center justify-center transition-colors">' +
                            '<span class="material-symbols-outlined text-[18px]">edit</span>' +
                        '</button>' +
                        '<button type="button" data-index="' + index + '" class="remove-btn w-8 h-8 rounded-full hover:bg-error-container text-error inline-flex items-center justify-center transition-colors">' +
                            '<span class="material-symbols-outlined text-[18px]">delete</span>' +
                        '</button>' +
                    '</div>' +
                '</td>';
            itensBody.appendChild(tr);
        });

        recalcular();
    }

    function totalVenda() {
        let total = 0;
        cart.forEach(function (item) { total += subtotalItem(item); });
        return total;
    }

    function recalcular() {
        const total = totalVenda();
        totalValor.textContent = formatBRL(total);

        const recebido = parseDecimal(valorRecebido.value);
        const troco = recebido - total;
        trocoValor.textContent = formatBRL(Math.max(troco, 0));
        trocoValor.classList.toggle("text-error", troco < 0 && metodoSelecionado() === "dinheiro");
        trocoValor.classList.toggle("text-primary", !(troco < 0 && metodoSelecionado() === "dinheiro"));

        atualizarSomaParcelas();
        renderParcelasValoresPadrao();

        const somaParcelas = somarParcelas();
        const parcelasFecham = Math.abs(somaParcelas - total) < 0.01;
        const precisaDinheiro = metodoSelecionado() === "dinheiro" && parcelas.length === 1 && total > 0;
        const podeFinalizar = cart.length > 0 && parcelasFecham && (!precisaDinheiro || recebido >= total);

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

    // ===== Passos Produto → Cliente → Pagamento (estilo Bling) =====

    const passoProdutoBtn = document.getElementById("passoProdutoBtn");
    const passoClienteBtn = document.getElementById("passoClienteBtn");
    const passoPagamentoBtn = document.getElementById("passoPagamentoBtn");
    const passoProduto = document.getElementById("passoProduto");
    const passoCliente = document.getElementById("passoCliente");
    const passoPagamento = document.getElementById("passoPagamento");
    const botoesPasso = { produto: passoProdutoBtn, cliente: passoClienteBtn, pagamento: passoPagamentoBtn };
    const painelPasso = { produto: passoProduto, cliente: passoCliente, pagamento: passoPagamento };
    const ordemPassos = ["produto", "cliente", "pagamento"];

    function ativarPasso(nome) {
        const indiceAtivo = ordemPassos.indexOf(nome);
        ordemPassos.forEach(function (passo, indice) {
            const btn = botoesPasso[passo];
            btn.classList.toggle("passo-ativo", indice === indiceAtivo);
            btn.classList.toggle("passo-completo", indice < indiceAtivo);
            painelPasso[passo].classList.toggle("hidden", indice !== indiceAtivo);
        });
        if (nome === "produto") buscaInput.focus();
        else if (nome === "cliente") document.getElementById("clienteNomeInput").focus();
    }

    passoProdutoBtn.addEventListener("click", function () { ativarPasso("produto"); });
    passoClienteBtn.addEventListener("click", function () { ativarPasso("cliente"); });
    passoPagamentoBtn.addEventListener("click", function () { ativarPasso("pagamento"); });

    // ===== Variações: produto com Formato=variacao pede escolha antes de editar =====

    const modalVariacao = document.getElementById("modalVariacao");
    const transicaoModalVariacao = UiTransicoes.modal(modalVariacao);
    const modalVariacaoTitulo = document.getElementById("modalVariacaoTitulo");
    const modalVariacaoLista = document.getElementById("modalVariacaoLista");
    const fecharModalVariacaoBtn = document.getElementById("fecharModalVariacaoBtn");

    function fecharModalVariacao() { transicaoModalVariacao.fechar(); }

    function abrirModalVariacao(produtoPai) {
        modalVariacaoTitulo.textContent = produtoPai.nome;
        modalVariacaoLista.innerHTML = "";
        produtoPai.variacoes.forEach(function (v) {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "w-full text-left px-md py-sm rounded-lg border border-outline-variant hover:border-secondary hover:bg-surface-container-low transition-colors flex items-center justify-between gap-md";

            const esgotado = v.saldoAtual <= 0;
            const nomeSpan = document.createElement("span");
            nomeSpan.textContent = (v.descricao || v.codigo) + (esgotado ? " (sem estoque)" : "");
            const precoSpan = document.createElement("span");
            precoSpan.className = "font-semibold text-secondary";
            precoSpan.textContent = formatBRL(v.precoUnitario);
            item.append(nomeSpan, precoSpan);

            item.addEventListener("click", function () {
                fecharModalVariacao();
                abrirEdicaoProduto({ id: v.id, codigo: v.codigo, nome: produtoPai.nome + " — " + (v.descricao || v.codigo), precoUnitario: v.precoUnitario, imagemUrl: produtoPai.imagemUrl });
            });
            modalVariacaoLista.appendChild(item);
        });
        transicaoModalVariacao.abrir();
    }

    if (fecharModalVariacaoBtn) fecharModalVariacaoBtn.addEventListener("click", fecharModalVariacao);
    if (modalVariacao) modalVariacao.addEventListener("click", function (e) { if (e.target === modalVariacao) fecharModalVariacao(); });

    // ===== Edição do produto escolhido: quantidade/desconto/valor/subtotal/comentário
    // antes de entrar no carrinho (mesmo passo intermediário do PDV do Bling). O card fica
    // sempre visível (nunca some do layout) — só alterna entre vazio/travado e
    // preenchido/editável; quem some é a lista de resultados, que sobrepõe a busca. =====

    const listaProduto = document.getElementById("listaProduto");
    const edicaoProduto = document.getElementById("edicaoProduto");
    const edicaoProdutoImg = document.getElementById("edicaoProdutoImg");
    const edicaoProdutoNome = document.getElementById("edicaoProdutoNome");
    const edicaoProdutoCodigo = document.getElementById("edicaoProdutoCodigo");
    const edicaoProdutoPreco = document.getElementById("edicaoProdutoPreco");
    const edicaoQtd = document.getElementById("edicaoQtd");
    const edicaoQtdDec = document.getElementById("edicaoQtdDec");
    const edicaoQtdInc = document.getElementById("edicaoQtdInc");
    const edicaoDesconto = document.getElementById("edicaoDesconto");
    const edicaoDescontoToggle = document.getElementById("edicaoDescontoToggle");
    const edicaoValorUnitario = document.getElementById("edicaoValorUnitario");
    const edicaoSubtotal = document.getElementById("edicaoSubtotal");
    const edicaoComentario = document.getElementById("edicaoComentario");
    const edicaoCancelarBtn = document.getElementById("edicaoCancelarBtn");
    const edicaoInserirBtn = document.getElementById("edicaoInserirBtn");
    const camposEdicaoProduto = [edicaoQtdDec, edicaoQtd, edicaoQtdInc, edicaoDesconto, edicaoDescontoToggle, edicaoValorUnitario, edicaoComentario, edicaoCancelarBtn, edicaoInserirBtn];

    let produtoEmEdicao = null;
    let descontoTipoEmEdicao = "percentual";
    let precoOriginalEmEdicao = 0;
    // Quando o desconto exibido veio do próprio Valor unitário editado (não digitado
    // pela pessoa), ele é só informativo — o Valor unitário já É o preço final, então
    // não pode ser descontado de novo no cálculo do subtotal (ver precoEdicaoComDesconto).
    let descontoOrigemAutomatica = false;

    function selecionarOuEscolherVariacao(produto) {
        if (produto.variacoes && produto.variacoes.length > 0) {
            abrirModalVariacao(produto);
            return;
        }
        abrirEdicaoProduto(produto);
    }

    function abrirEdicaoProduto(produto) {
        produtoEmEdicao = produto;
        descontoTipoEmEdicao = "percentual";
        descontoOrigemAutomatica = false;
        precoOriginalEmEdicao = produto.precoUnitario;
        edicaoProdutoImg.innerHTML = miniaturaHtml(produto.imagemUrl, 48);
        edicaoProdutoNome.textContent = produto.nome;
        edicaoProdutoCodigo.textContent = produto.codigo;
        edicaoProdutoPreco.textContent = formatBRL(produto.precoUnitario);
        edicaoQtd.value = "1";
        edicaoDesconto.value = "";
        edicaoDescontoToggle.textContent = "%";
        edicaoValorUnitario.value = Number(produto.precoUnitario).toFixed(2);
        edicaoComentario.value = "";

        edicaoProduto.classList.remove("card-vazio");
        camposEdicaoProduto.forEach(function (el) { el.disabled = false; });
        atualizarEdicaoSubtotal();
        fecharResultadosProduto();
        edicaoQtd.focus();
    }

    function fecharEdicaoProduto() {
        produtoEmEdicao = null;
        descontoOrigemAutomatica = false;
        edicaoProdutoImg.innerHTML = '<span class="material-symbols-outlined text-[48px] text-outline-variant">inventory_2</span>';
        edicaoProdutoNome.textContent = "Nenhum produto selecionado";
        edicaoProdutoCodigo.textContent = "Busque um produto acima";
        edicaoProdutoPreco.textContent = "R$ 0,00";
        edicaoQtd.value = "1";
        edicaoDesconto.value = "0,00";
        edicaoDescontoToggle.textContent = "%";
        edicaoValorUnitario.value = "";
        edicaoComentario.value = "";
        edicaoSubtotal.textContent = "R$ 0,00";

        edicaoProduto.classList.add("card-vazio");
        camposEdicaoProduto.forEach(function (el) { el.disabled = true; });
        buscaInput.value = "";
        buscaInput.focus();
        fecharResultadosProduto();
    }

    // O Valor unitário sempre manda no subtotal. Quando o desconto exibido veio da própria
    // edição do valor unitário (ver listener abaixo), ele é só um indicador da diferença
    // pro preço de tabela — não é reaplicado por cima, senão descontaria duas vezes. Só um
    // desconto digitado à mão pela pessoa desconta de fato sobre o valor unitário atual.
    function precoEdicaoComDesconto() {
        const valorUnitario = parseDecimal(edicaoValorUnitario.value);
        const desconto = Math.max(parseDecimal(edicaoDesconto.value), 0);
        if (!desconto || descontoOrigemAutomatica) return valorUnitario;
        if (descontoTipoEmEdicao === "valor") return Math.max(valorUnitario - desconto, 0);
        return valorUnitario * (1 - Math.min(desconto, 100) / 100);
    }

    function atualizarEdicaoSubtotal() {
        const qtd = Math.max(parseInt(edicaoQtd.value, 10) || 1, 1);
        edicaoSubtotal.textContent = formatBRL(qtd * precoEdicaoComDesconto());
    }

    edicaoQtd.addEventListener("input", atualizarEdicaoSubtotal);
    edicaoValorUnitario.addEventListener("input", atualizarEdicaoSubtotal);

    // Editar o desconto à mão sempre desconta "de verdade" sobre o valor unitário atual
    // — só o preenchimento automático (abaixo) fica marcado como não reaplicável.
    edicaoDesconto.addEventListener("input", function () {
        descontoOrigemAutomatica = false;
        atualizarEdicaoSubtotal();
    });

    // Editar o valor unitário direto (ex: baixar de R$290 pra R$280 num acerto de preço)
    // preenche o desconto sozinho com essa diferença, no modo % ou R$ que já estiver
    // selecionado — sem isso a pessoa precisaria calcular o desconto de cabeça toda vez.
    // O valor unitário digitado já É o preço final: o desconto aqui é só informativo.
    edicaoValorUnitario.addEventListener("input", function () {
        const valorAtual = parseDecimal(edicaoValorUnitario.value);
        descontoOrigemAutomatica = true;
        if (!precoOriginalEmEdicao || valorAtual >= precoOriginalEmEdicao) {
            edicaoDesconto.value = "";
            return;
        }
        const diferenca = precoOriginalEmEdicao - valorAtual;
        if (descontoTipoEmEdicao === "valor") {
            edicaoDesconto.value = diferenca.toFixed(2);
        } else {
            edicaoDesconto.value = (diferenca / precoOriginalEmEdicao * 100).toFixed(2);
        }
    });

    edicaoQtdDec.addEventListener("click", function () {
        edicaoQtd.value = Math.max((parseInt(edicaoQtd.value, 10) || 1) - 1, 1);
        atualizarEdicaoSubtotal();
    });
    edicaoQtdInc.addEventListener("click", function () {
        edicaoQtd.value = (parseInt(edicaoQtd.value, 10) || 1) + 1;
        atualizarEdicaoSubtotal();
    });

    // Alterna o desconto entre % e R$ recalculando o valor equivalente em vez de zerar —
    // troca só a "unidade" exibida, a diferença de preço que ela representa continua a
    // mesma (10% de R$290 vira R$29,00 e vice-versa), esteja esse desconto ali por causa do
    // valor unitário editado (automático) ou digitado à mão. A base do percentual muda
    // conforme a origem: desconto automático é sempre sobre o preço original (é dali que a
    // diferença foi calculada); desconto manual é sobre o valor unitário atual (o que
    // realmente vai ser descontado na hora de inserir).
    edicaoDescontoToggle.addEventListener("click", function () {
        const baseCalculo = descontoOrigemAutomatica ? precoOriginalEmEdicao : parseDecimal(edicaoValorUnitario.value);
        const descontoAtual = Math.max(parseDecimal(edicaoDesconto.value), 0);

        let diferencaEmReais;
        if (descontoTipoEmEdicao === "valor") {
            diferencaEmReais = descontoAtual;
        } else {
            diferencaEmReais = baseCalculo * Math.min(descontoAtual, 100) / 100;
        }

        descontoTipoEmEdicao = descontoTipoEmEdicao === "valor" ? "percentual" : "valor";
        edicaoDescontoToggle.textContent = descontoTipoEmEdicao === "valor" ? "R$" : "%";

        if (!descontoAtual) {
            edicaoDesconto.value = "";
        } else if (descontoTipoEmEdicao === "valor") {
            edicaoDesconto.value = diferencaEmReais.toFixed(2);
        } else if (baseCalculo > 0) {
            edicaoDesconto.value = Math.min(diferencaEmReais / baseCalculo * 100, 100).toFixed(2);
        } else {
            edicaoDesconto.value = "";
        }
        atualizarEdicaoSubtotal();
    });

    edicaoCancelarBtn.addEventListener("click", fecharEdicaoProduto);

    edicaoInserirBtn.addEventListener("click", function () {
        if (!produtoEmEdicao) return;
        const qtd = Math.max(parseInt(edicaoQtd.value, 10) || 1, 1);
        const valorUnitario = parseDecimal(edicaoValorUnitario.value);
        const comentario = edicaoComentario.value.trim();

        // O carrinho guarda sempre desconto percentual (mesmo padrão do resto da venda);
        // se foi digitado em R$ aqui, converte na hora de inserir. Quando o desconto exibido
        // é só o reflexo automático de ter editado o valor unitário direto, ele já está
        // embutido no preço — gravar de novo descontaria duas vezes (ver precoEdicaoComDesconto).
        let percentual = 0;
        if (!descontoOrigemAutomatica) {
            percentual = Math.max(parseDecimal(edicaoDesconto.value), 0);
            if (descontoTipoEmEdicao === "valor" && valorUnitario > 0) {
                percentual = Math.min(percentual / valorUnitario * 100, 100);
            } else {
                percentual = Math.min(percentual, 100);
            }
        }

        const existente = cart.find(function (i) { return i.codigo === produtoEmEdicao.codigo && i.comentario === comentario; });
        if (existente) {
            existente.qtd += qtd;
            existente.desconto = percentual;
            existente.precoUnitario = valorUnitario;
        } else {
            cart.push({
                id: produtoEmEdicao.id, codigo: produtoEmEdicao.codigo, nome: produtoEmEdicao.nome,
                precoUnitario: valorUnitario, qtd: qtd, desconto: percentual, descontoTipo: "percentual", comentario: comentario,
            });
        }
        renderTabela();
        fecharEdicaoProduto();
    });

    // ===== Editar item já no carrinho: reabre a área de edição pré-preenchida =====

    itensBody.addEventListener("click", function (e) {
        const editarBtn = e.target.closest(".editar-item-btn");
        if (editarBtn) {
            const index = parseInt(editarBtn.dataset.index, 10);
            const item = cart[index];
            const produtoCatalogo = porId(item.id);
            cart.splice(index, 1);
            renderTabela();
            ativarPasso("produto");
            // precoOriginalEmEdicao (definido dentro de abrirEdicaoProduto) precisa ser o
            // preço de catálogo, não o preço já com desconto do item — senão o auto-cálculo
            // do desconto ao reeditar o valor unitário parte de uma base errada.
            abrirEdicaoProduto({ id: item.id, codigo: item.codigo, nome: item.nome, precoUnitario: produtoCatalogo ? produtoCatalogo.precoUnitario : item.precoUnitario, imagemUrl: produtoCatalogo && produtoCatalogo.imagemUrl });
            edicaoValorUnitario.value = Number(item.precoUnitario).toFixed(2);
            edicaoQtd.value = item.qtd;
            edicaoDesconto.value = item.desconto || "";
            descontoOrigemAutomatica = false;
            edicaoComentario.value = item.comentario || "";
            atualizarEdicaoSubtotal();
            return;
        }

        const removeBtn = e.target.closest(".remove-btn");
        if (removeBtn) {
            const index = parseInt(removeBtn.dataset.index, 10);
            cart.splice(index, 1);
            renderTabela();
        }
    });

    // ===== Resultados de produto: dropdown que sobrepõe a busca (o card de edição, logo
    // abaixo, fica sempre visível — ver ponto 2 do pedido) =====

    const resultadosProduto = document.getElementById("resultadosProduto");
    const resultadosProdutoVazio = document.getElementById("resultadosProdutoVazio");

    function fecharResultadosProduto() {
        listaProduto.classList.add("hidden");
    }

    function renderResultadosProduto() {
        const encontrados = buscarProdutos(buscaInput.value);
        listaProduto.classList.toggle("hidden", encontrados.length === 0 && !buscaInput.value.trim());
        resultadosProdutoVazio.classList.toggle("hidden", encontrados.length > 0);
        resultadosProduto.innerHTML = "";

        encontrados.forEach(function (p) {
            const item = document.createElement("button");
            item.type = "button";
            item.className = "resultado-produto-item w-full text-left px-sm py-sm rounded-lg hover:bg-surface-container-low transition-colors flex items-center gap-sm";

            const miniatura = document.createElement("div");
            miniatura.className = "w-11 h-11 shrink-0 rounded-lg bg-surface-container-low flex items-center justify-center overflow-hidden";
            miniatura.innerHTML = miniaturaHtml(p.imagemUrl, 22);

            const conteudo = document.createElement("div");
            conteudo.className = "flex-1 min-w-0";

            const nome = document.createElement("p");
            nome.className = "font-body-md text-body-md text-on-surface truncate flex items-center gap-xs";
            const nomeTexto = document.createElement("span");
            nomeTexto.className = "truncate";
            nomeTexto.textContent = p.nome;
            nome.appendChild(nomeTexto);
            if (p._compativelDe) {
                const etiqueta = document.createElement("span");
                etiqueta.className = "shrink-0 bg-secondary-container text-on-secondary-container font-label-sm text-label-sm px-xs rounded";
                etiqueta.title = "Também serve no mesmo aparelho que " + p._compativelDe;
                etiqueta.textContent = "compatível";
                nome.appendChild(etiqueta);
            }
            const detalhe = document.createElement("p");
            detalhe.className = "font-label-sm text-label-sm text-outline";
            detalhe.textContent = p.codigo + " · estoque: " + p.saldoAtual;
            conteudo.append(nome, detalhe);

            const preco = document.createElement("span");
            preco.className = "font-body-md text-body-md font-semibold text-secondary shrink-0";
            preco.textContent = formatBRL(p.precoUnitario);

            item.append(miniatura, conteudo, preco);
            item.addEventListener("click", function () { selecionarOuEscolherVariacao(p); });
            resultadosProduto.appendChild(item);
        });
    }

    buscaInput.addEventListener("input", renderResultadosProduto);

    buscaInput.addEventListener("keydown", function (e) {
        if (e.key !== "Enter") return;
        const primeiro = resultadosProduto.querySelector(".resultado-produto-item");
        if (!primeiro) return;
        e.preventDefault();
        primeiro.click();
    });

    buscaForm.addEventListener("submit", function (e) {
        e.preventDefault();
        const primeiro = resultadosProduto.querySelector(".resultado-produto-item");
        if (primeiro) primeiro.click();
    });

    document.addEventListener("click", function (e) {
        if (!buscaForm.contains(e.target)) fecharResultadosProduto();
    });

    // ===== Passo Cliente: texto livre, sem puxar do cadastro =====

    const clienteNomeInput = document.getElementById("clienteNomeInput");
    const clienteTelefoneInput = document.getElementById("clienteTelefoneInput");
    const clienteDocumentoInput = document.getElementById("clienteDocumentoInput");
    const clientePassoMarcador = document.getElementById("clientePassoMarcador");
    const clienteNomePost = document.getElementById("clienteNomePost");
    const clienteTelefonePost = document.getElementById("clienteTelefonePost");
    const clienteDocumentoPost = document.getElementById("clienteDocumentoPost");

    function atualizarMarcadorCliente() {
        const preenchido = clienteNomeInput.value.trim() || clienteTelefoneInput.value.trim() || clienteDocumentoInput.value.trim();
        clientePassoMarcador.classList.toggle("hidden", !preenchido);
    }

    [clienteNomeInput, clienteTelefoneInput, clienteDocumentoInput].forEach(function (input) {
        input.addEventListener("input", atualizarMarcadorCliente);
    });

    if (typeof CLIENTE_EM_EDICAO !== "undefined" && CLIENTE_EM_EDICAO) {
        clienteNomeInput.value = CLIENTE_EM_EDICAO.nome || "";
        clienteTelefoneInput.value = CLIENTE_EM_EDICAO.telefone || "";
        clienteDocumentoInput.value = CLIENTE_EM_EDICAO.documento || "";
        atualizarMarcadorCliente();
    }

    // ===== Passo Pagamento: forma da 1ª parcela + tabela de parcelas independentes =====

    function metodoSelecionado() {
        const marcado = document.querySelector('input[name="metodo"]:checked');
        return marcado ? marcado.value : "dinheiro";
    }

    function atualizarSelecaoPagamento() {
        document.querySelectorAll(".card-pagamento").forEach(function (label) {
            const input = label.querySelector('input[type="radio"]');
            label.classList.toggle("selecionado", input.checked);
        });
        painelDinheiro.classList.toggle("hidden", metodoSelecionado() !== "dinheiro" || parcelas.length > 1);
    }

    // Cada parcela é independente: dias, data, valor, forma e observação próprios —
    // não precisam somar igual entre si, só a soma total precisa bater com o Total.
    let parcelas = (typeof PARCELAS_EM_EDICAO !== "undefined" && PARCELAS_EM_EDICAO.length > 0)
        ? PARCELAS_EM_EDICAO.map(function (p) { return { dias: p.dias, data: p.data, valor: p.valor, forma: p.formaPagamento, observacao: p.observacao || "" }; })
        : [{ dias: 0, data: dataHojeIso(), valor: 0, forma: "dinheiro", observacao: "" }];

    function dataHojeIso() {
        const d = new Date();
        return d.getFullYear() + "-" + String(d.getMonth() + 1).padStart(2, "0") + "-" + String(d.getDate()).padStart(2, "0");
    }

    const parcelasBody = document.getElementById("parcelasBody");
    const adicionarParcelaBtn = document.getElementById("adicionarParcelaBtn");
    const parcelasSomaAviso = document.getElementById("parcelasSomaAviso");

    function somarParcelas() {
        return parcelas.reduce(function (soma, p) { return soma + (parseDecimal(p.valor) || 0); }, 0);
    }

    // Enquanto só existe 1 parcela, o valor dela acompanha o total automaticamente
    // (não faz sentido pedir pra digitar de novo o que já está na venda) — sincroniza
    // tanto o array quanto o input somente-leitura já renderizado, sem precisar
    // recriar a linha inteira. Ao adicionar mais parcelas, cada uma vira 100%
    // independente e editável.
    function renderParcelasValoresPadrao() {
        if (parcelas.length !== 1) return;
        parcelas[0].valor = totalVenda();
        const input = parcelasBody.querySelector('.parcela-valor[data-index="0"]');
        if (input && document.activeElement !== input) input.value = Number(parcelas[0].valor).toFixed(2);
    }

    function atualizarSomaParcelas() {
        const total = totalVenda();
        const soma = somarParcelas();
        const fecha = Math.abs(soma - total) < 0.01;
        parcelasSomaAviso.classList.toggle("hidden", fecha || cart.length === 0);
        if (!fecha) {
            parcelasSomaAviso.textContent = "Parcelas somam " + formatBRL(soma) + ", total é " + formatBRL(total) + ".";
        }
    }

    function renderParcelas() {
        parcelasBody.innerHTML = "";
        parcelas.forEach(function (parcela, index) {
            const linha = document.createElement("div");
            linha.className = "grid grid-cols-12 gap-xs items-end bg-surface-container-low rounded-lg p-sm";
            linha.dataset.index = String(index);
            linha.innerHTML =
                '<div class="col-span-1 font-label-sm text-label-sm text-outline text-center self-center">' + (index + 1) + '</div>' +
                '<div class="col-span-3">' +
                    '<label class="font-label-sm text-label-sm text-outline">Data</label>' +
                    '<input type="date" data-index="' + index + '" class="parcela-data w-full mt-xs bg-white border border-outline-variant rounded-md px-xs py-1 font-label-sm text-label-sm" value="' + parcela.data + '" />' +
                '</div>' +
                '<div class="col-span-3">' +
                    '<label class="font-label-sm text-label-sm text-outline">Valor</label>' +
                    '<input type="number" min="0" step="0.01" data-index="' + index + '" class="parcela-valor w-full mt-xs bg-white border border-outline-variant rounded-md px-xs py-1 font-label-sm text-label-sm" value="' + Number(parcela.valor).toFixed(2) + '" ' + (parcelas.length === 1 ? "readonly" : "") + ' />' +
                '</div>' +
                '<div class="col-span-3">' +
                    '<label class="font-label-sm text-label-sm text-outline">Forma</label>' +
                    '<select data-index="' + index + '" class="parcela-forma w-full mt-xs bg-white border border-outline-variant rounded-md px-xs py-1 font-label-sm text-label-sm">' +
                        '<option value="dinheiro"' + (parcela.forma === "dinheiro" ? " selected" : "") + '>Dinheiro</option>' +
                        '<option value="cartao"' + (parcela.forma === "cartao" ? " selected" : "") + '>Cartão</option>' +
                        '<option value="pix"' + (parcela.forma === "pix" ? " selected" : "") + '>PIX</option>' +
                    '</select>' +
                '</div>' +
                '<div class="col-span-2 flex justify-end">' +
                    (parcelas.length > 1
                        ? '<button type="button" data-index="' + index + '" class="remover-parcela-btn w-8 h-8 rounded-full hover:bg-error-container text-error inline-flex items-center justify-center transition-colors"><span class="material-symbols-outlined text-[18px]">delete</span></button>'
                        : '') +
                '</div>';
            parcelasBody.appendChild(linha);
        });
        atualizarSelecaoPagamento();
    }

    adicionarParcelaBtn.addEventListener("click", function () {
        const total = totalVenda();
        const somaAtual = somarParcelas();
        parcelas.push({ dias: 0, data: dataHojeIso(), valor: Math.max(total - somaAtual, 0), forma: metodoSelecionado(), observacao: "" });
        renderParcelas();
        recalcular();
    });

    parcelasBody.addEventListener("click", function (e) {
        const removerBtn = e.target.closest(".remover-parcela-btn");
        if (!removerBtn) return;
        const index = parseInt(removerBtn.dataset.index, 10);
        parcelas.splice(index, 1);
        renderParcelas();
        recalcular();
    });

    parcelasBody.addEventListener("input", function (e) {
        const index = parseInt(e.target.dataset.index, 10);
        if (isNaN(index)) return;
        if (e.target.classList.contains("parcela-data")) parcelas[index].data = e.target.value;
        if (e.target.classList.contains("parcela-valor")) parcelas[index].valor = parseDecimal(e.target.value);
        recalcular();
    });

    parcelasBody.addEventListener("change", function (e) {
        const index = parseInt(e.target.dataset.index, 10);
        if (isNaN(index)) return;
        if (e.target.classList.contains("parcela-forma")) parcelas[index].forma = e.target.value;
        recalcular();
    });

    document.querySelectorAll('input[name="metodo"]').forEach(function (input) {
        input.addEventListener("change", function () {
            if (parcelas.length === 1) parcelas[0].forma = metodoSelecionado();
            atualizarSelecaoPagamento();
            recalcular();
        });
    });

    valorRecebido.addEventListener("input", recalcular);

    if (typeof VALOR_RECEBIDO_EM_EDICAO !== "undefined" && VALOR_RECEBIDO_EM_EDICAO > 0) {
        valorRecebido.value = Number(VALOR_RECEBIDO_EM_EDICAO).toFixed(2);
    }
    if (parcelas.length > 0) {
        const radioForma = document.querySelector('input[name="metodo"][value="' + parcelas[0].forma + '"]');
        if (radioForma) radioForma.checked = true;
    }

    // ===== Atalhos Alt+ (Bling): Z produto, C cliente, B pagamento, N nova venda,
    // Q excluir/limpar, S finalizar. accesskey já cobre a maioria; F2 mantido à parte
    // porque "acessar busca" é o atalho mais usado no dia a dia. =====

    document.addEventListener("keydown", function (e) {
        if (e.key === "F2") {
            e.preventDefault();
            ativarPasso("produto");
        }
    });

    const excluirVendaBtn = document.getElementById("excluirVendaBtn");
    if (excluirVendaBtn) {
        excluirVendaBtn.addEventListener("click", function () {
            if (cart.length === 0) return;
            if (!confirm("Limpar todos os itens desta venda?")) return;
            cart.length = 0;
            renderTabela();
        });
    }

    atualizarSelecaoPagamento();
    renderTabela();
    renderResultadosProduto();
    renderParcelas();
    ativarPasso("produto");

    // ===== Envio da venda =====

    const itensPost = document.getElementById("itensPost");
    const parcelasPost = document.getElementById("parcelasPost");
    const valorRecebidoPost = document.getElementById("valorRecebidoPost");

    function oculto(container, nome, valor) {
        const i = document.createElement("input");
        i.type = "hidden"; i.name = nome; i.value = valor;
        container.appendChild(i);
    }

    function preencherCamposOcultos() {
        itensPost.innerHTML = "";
        cart.forEach(function (item) {
            oculto(itensPost, "itemProdutoId", item.id);
            oculto(itensPost, "itemQuantidade", item.qtd);
            oculto(itensPost, "itemPreco", Number(item.precoUnitario).toFixed(2));
            oculto(itensPost, "itemDesconto", Number(item.desconto || 0).toFixed(2));
            oculto(itensPost, "itemComentario", item.comentario || "");
        });

        parcelasPost.innerHTML = "";
        parcelas.forEach(function (p) {
            oculto(parcelasPost, "parcelaDias", p.dias || 0);
            oculto(parcelasPost, "parcelaData", p.data);
            oculto(parcelasPost, "parcelaValor", Number(p.valor).toFixed(2));
            oculto(parcelasPost, "parcelaForma", p.forma);
            oculto(parcelasPost, "parcelaObservacao", p.observacao || "");
        });

        const recebido = parseDecimal(valorRecebido.value);
        valorRecebidoPost.value = Number(recebido).toFixed(2);

        clienteNomePost.value = clienteNomeInput.value.trim();
        clienteTelefonePost.value = clienteTelefoneInput.value.trim();
        clienteDocumentoPost.value = clienteDocumentoInput.value.trim();
    }

    // ===== Painel de fechamento: só existe para venda nova (VENDA_EM_EDICAO_ID === 0).
    // Edição de venda continua com o form tradicional — mais simples, e uma edição já
    // parte de uma venda que existe, não precisa do ritual de "fechar" de novo. =====

    const veuFechamento = document.getElementById("veuFechamento");
    const painelFechamento = document.getElementById("painelFechamento");
    const painelVendaNumero = document.getElementById("painelVendaNumero");
    const painelItens = document.getElementById("painelItens");
    const painelTotal = document.getElementById("painelTotal");
    const painelAviso = document.getElementById("painelAviso");
    const painelReciboBtn = document.getElementById("painelReciboBtn");
    const fecharVendaBtn = document.getElementById("fecharVendaBtn");

    function rotuloFormaPagamento(forma) {
        if (forma === "cartao") return "Cartão";
        if (forma === "pix") return "PIX";
        return "Dinheiro";
    }

    function abrirPainelFechamento(venda) {
        painelVendaNumero.textContent = "Venda " + venda.numero + " · " + rotuloFormaPagamento(venda.formaPagamento);
        painelItens.innerHTML = "";
        venda.itens.forEach(function (item) {
            const linha = document.createElement("div");
            linha.className = "flex justify-between gap-sm";
            const desc = document.createElement("span");
            desc.className = "truncate";
            desc.textContent = item.quantidade + "x " + item.descricao;
            const valor = document.createElement("span");
            valor.className = "font-semibold shrink-0";
            valor.textContent = formatBRL(item.total);
            linha.append(desc, valor);
            painelItens.appendChild(linha);
        });
        painelTotal.textContent = formatBRL(venda.total);

        if (venda.aviso) {
            painelAviso.textContent = venda.aviso;
            painelAviso.classList.remove("hidden");
        } else {
            painelAviso.classList.add("hidden");
        }

        painelReciboBtn.href = "/Caixa/Recibo/" + venda.id;

        veuFechamento.classList.remove("hidden");
        requestAnimationFrame(function () {
            veuFechamento.classList.remove("opacity-0");
            painelFechamento.classList.remove("fechado");
        });
    }

    function fecharPainelFechamento() {
        veuFechamento.classList.add("opacity-0");
        painelFechamento.classList.add("fechado");
        window.setTimeout(function () { veuFechamento.classList.add("hidden"); }, 220);

        // Esvazia o carrinho e volta pronto pra próxima venda
        cart.length = 0;
        clienteNomeInput.value = "";
        clienteTelefoneInput.value = "";
        clienteDocumentoInput.value = "";
        atualizarMarcadorCliente();
        valorRecebido.value = "";
        parcelas = [{ dias: 0, data: dataHojeIso(), valor: 0, forma: "dinheiro", observacao: "" }];
        document.querySelector('input[name="metodo"][value="dinheiro"]').checked = true;
        renderParcelas();
        atualizarSelecaoPagamento();
        renderTabela();
        ativarPasso("produto");
    }

    if (fecharVendaBtn) fecharVendaBtn.addEventListener("click", fecharPainelFechamento);

    if (finalizarBtn && finalizarBtn.form) {
        finalizarBtn.form.addEventListener("submit", function (e) {
            if (cart.length === 0) { e.preventDefault(); return; }

            e.preventDefault();
            preencherCamposOcultos();

            if (VENDA_EM_EDICAO_ID > 0) {
                // Edição de venda: segue o form tradicional (POST + redirect para Vendas)
                e.target.submit();
                return;
            }

            finalizarBtn.disabled = true;
            const formData = new FormData(e.target);
            fetch(e.target.action, {
                method: "POST",
                headers: { "X-Requested-With": "XMLHttpRequest" },
                body: formData,
            })
                .then(function (r) {
                    if (!r.ok) return r.json().then(function (d) { throw new Error(d.erro || "Não foi possível finalizar a venda."); });
                    return r.json();
                })
                .then(function (venda) {
                    abrirPainelFechamento(venda);
                })
                .catch(function (err) {
                    mostrarToast(err.message, true);
                })
                .finally(function () {
                    recalcular();
                });
        });
    }
});
