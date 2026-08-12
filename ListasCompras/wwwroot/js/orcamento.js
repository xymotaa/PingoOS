document.addEventListener("DOMContentLoaded", function () {
    const itensBody = document.getElementById("itensBody");
    const itensVazio = document.getElementById("itensVazio");
    const totalGeral = document.getElementById("totalGeral");
    const adicionarItemBtn = document.getElementById("adicionarItemBtn");
    const toast = document.getElementById("toast");
    const toastMsg = document.getElementById("toastMsg");
    const toastIcon = document.getElementById("toastIcon");

    function formatBRL(valor) {
        return "R$ " + valor.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function parseDecimal(valor) {
        return parseFloat(String(valor).replace(/\./g, "").replace(",", ".")) || 0;
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

    function criarLinha() {
        const tr = document.createElement("tr");
        tr.className = "border-t border-outline-variant";
        tr.innerHTML =
            '<td class="px-md py-sm">' +
                '<div class="relative">' +
                    '<input type="text" name="itemDescricao" class="item-desc w-full bg-surface-container-low border-none rounded-lg pl-md pr-10 py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" placeholder="Digite ou escolha do catálogo" />' +
                    '<button type="button" class="escolher-servico absolute right-2 top-1/2 -translate-y-1/2 w-7 h-7 rounded-lg hover:bg-surface-container-high flex items-center justify-center text-outline transition-colors" title="Escolher do catálogo de serviços">' +
                        '<span class="material-symbols-outlined text-[20px]">handyman</span>' +
                    '</button>' +
                '</div>' +
            '</td>' +
            '<td class="px-md py-sm">' +
                '<input type="text" name="itemQuantidade" inputmode="numeric" value="1" class="item-qtd w-full text-center bg-surface-container-low border-none rounded-lg px-2 py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" />' +
            '</td>' +
            '<td class="px-md py-sm">' +
                '<input type="text" inputmode="decimal" placeholder="0,00" class="item-valor w-full text-right bg-surface-container-low border-none rounded-lg px-md py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" />' +
                // O visível aceita vírgula; o que vai para o servidor usa ponto, senão o binding do .NET recusa
                '<input type="hidden" name="itemValor" class="item-valor-post" value="0" />' +
            '</td>' +
            '<td class="px-md py-sm text-right">' +
                '<span class="item-total font-body-md text-body-md font-semibold text-secondary">R$ 0,00</span>' +
            '</td>' +
            '<td class="pr-md">' +
                '<button type="button" class="remover-item w-8 h-8 rounded-full hover:bg-error-container inline-flex items-center justify-center text-outline transition-colors" title="Remover item">' +
                    '<span class="material-symbols-outlined text-[18px]">delete</span>' +
                '</button>' +
            '</td>';
        return tr;
    }

    function atualizarVazio() {
        const vazio = itensBody.children.length === 0;
        itensVazio.classList.toggle("hidden", !vazio);
    }

    function recalcular() {
        let total = 0;
        itensBody.querySelectorAll("tr").forEach(function (tr) {
            const qtd = parseInt(tr.querySelector(".item-qtd").value, 10) || 0;
            const valor = parseDecimal(tr.querySelector(".item-valor").value);
            const subtotal = qtd * valor;
            tr.querySelector(".item-total").textContent = formatBRL(subtotal);
            tr.querySelector(".item-valor-post").value = valor.toFixed(2);
            total += subtotal;
        });
        totalGeral.textContent = formatBRL(total);
    }

    function adicionarItem(foco) {
        const tr = criarLinha();
        itensBody.appendChild(tr);
        atualizarVazio();
        recalcular();
        if (foco) tr.querySelector(".item-desc").focus();
    }

    adicionarItemBtn.addEventListener("click", function () { adicionarItem(true); });

    itensBody.addEventListener("input", function (e) {
        if (e.target.matches(".item-qtd") || e.target.matches(".item-valor")) recalcular();
    });

    itensBody.addEventListener("click", function (e) {
        const remover = e.target.closest(".remover-item");
        if (!remover) return;
        remover.closest("tr").remove();
        atualizarVazio();
        recalcular();
    });


    // ===== Aparelhos: o cliente pode deixar mais de um na mesma visita =====

    const aparelhosBody = document.getElementById("aparelhosBody");
    const adicionarAparelhoBtn = document.getElementById("adicionarAparelhoBtn");
    const aparelhosLimite = document.getElementById("aparelhosLimite");
    const maxAparelhos = (typeof MAX_APARELHOS === "number") ? MAX_APARELHOS : 5;

    function blocoAparelho(dados, indice) {
        const d = dados || { tipo: "", marca: "", modelo: "", serie: "", semSerie: false };
        const div = document.createElement("div");
        div.className = "bloco-aparelho";
        div.innerHTML =
            '<div class="flex items-center justify-between gap-sm mb-xs">' +
                '<span class="font-label-md text-label-md text-outline uppercase tracking-wider">Aparelho ' + (indice + 1) + '</span>' +
                '<button type="button" class="remover-aparelho w-7 h-7 rounded-lg hover:bg-error-container inline-flex items-center justify-center text-outline transition-colors" title="Remover este aparelho">' +
                    '<span class="material-symbols-outlined text-[18px]">delete</span>' +
                '</button>' +
            '</div>' +
            '<div class="grid grid-cols-1 md:grid-cols-12 gap-md">' +
                campo(3, "Tipo de aparelho", "aparelhoTipo", d.tipo, "Ex: Celular, Tablet, Notebook") +
                campo(3, "Marca", "aparelhoMarca", d.marca, "Ex: Samsung, Apple, Xiaomi") +
                campo(3, "Modelo", "aparelhoModelo", d.modelo, "Ex: Galaxy A54") +
                '<div class="md:col-span-3">' +
                    '<div class="flex items-center justify-between gap-sm">' +
                        '<label class="font-label-md text-label-md text-on-surface-variant">Número de série / IMEI</label>' +
                        '<label class="flex items-center gap-xs cursor-pointer shrink-0">' +
                            '<input type="checkbox" class="sem-serie w-4 h-4 accent-secondary"' + (d.semSerie ? " checked" : "") + ' />' +
                            '<span class="font-label-sm text-label-sm text-outline">Sem número</span>' +
                        '</label>' +
                    '</div>' +
                    '<input type="text" name="aparelhoSerie" value="' + escapar(d.serie) + '" class="serie w-full mt-xs bg-surface-container-low border-none rounded-lg px-md py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30 disabled:opacity-50" />' +
                    '<input type="hidden" name="aparelhoSemSerie" class="sem-serie-post" value="' + (d.semSerie ? "1" : "0") + '" />' +
                '</div>' +
            '</div>';
        return div;
    }

    function campo(cols, rotulo, nome, valor, dica) {
        return '<div class="md:col-span-' + cols + '">' +
            '<label class="font-label-md text-label-md text-on-surface-variant">' + rotulo + '</label>' +
            '<input type="text" name="' + nome + '" value="' + escapar(valor) + '" placeholder="' + dica + '" ' +
            'class="w-full mt-xs bg-surface-container-low border-none rounded-lg px-md py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" />' +
        '</div>';
    }

    function escapar(v) {
        return String(v || "").replace(/&/g, "&amp;").replace(/"/g, "&quot;").replace(/</g, "&lt;");
    }

    function renumerarAparelhos() {
        const blocos = aparelhosBody.querySelectorAll(".bloco-aparelho");
        blocos.forEach(function (b, i) {
            b.querySelector("span").textContent = "Aparelho " + (i + 1);
            // Com um aparelho só não faz sentido oferecer remover
            b.querySelector(".remover-aparelho").classList.toggle("hidden", blocos.length === 1);
            b.classList.toggle("pt-md", i > 0);
            b.classList.toggle("border-t", i > 0);
            b.classList.toggle("border-outline-variant", i > 0);
        });
        adicionarAparelhoBtn.classList.toggle("hidden", blocos.length >= maxAparelhos);
        aparelhosLimite.classList.toggle("hidden", blocos.length < maxAparelhos);
    }

    function adicionarAparelho(dados) {
        const blocos = aparelhosBody.querySelectorAll(".bloco-aparelho").length;
        if (blocos >= maxAparelhos) return;
        aparelhosBody.appendChild(blocoAparelho(dados, blocos));
        renumerarAparelhos();
    }

    adicionarAparelhoBtn.addEventListener("click", function () { adicionarAparelho(null); });

    aparelhosBody.addEventListener("click", function (e) {
        const remover = e.target.closest(".remover-aparelho");
        if (!remover) return;
        remover.closest(".bloco-aparelho").remove();
        renumerarAparelhos();
    });

    // "Sem número de série": desabilita o campo e marca o que vai para o servidor
    aparelhosBody.addEventListener("change", function (e) {
        if (!e.target.matches(".sem-serie")) return;
        const bloco = e.target.closest(".bloco-aparelho");
        const serie = bloco.querySelector(".serie");
        // readonly em vez de disabled: campo desabilitado não é enviado e desalinharia
        // os arrays de aparelhos no servidor
        serie.readOnly = e.target.checked;
        serie.classList.toggle("opacity-50", e.target.checked);
        if (e.target.checked) serie.value = "";
        bloco.querySelector(".sem-serie-post").value = e.target.checked ? "1" : "0";
    });

    (typeof APARELHOS !== "undefined" && APARELHOS.length ? APARELHOS : [null]).forEach(adicionarAparelho);
    aparelhosBody.querySelectorAll(".sem-serie:checked").forEach(function (c) {
        const serie = c.closest(".bloco-aparelho").querySelector(".serie");
        serie.readOnly = true;
        serie.classList.add("opacity-50");
    });

    // ===== Cliente: os dados vêm do cadastro, não são digitados aqui =====

    const modalCliente = document.getElementById("modalCliente");
    const buscarClienteBtn = document.getElementById("buscarClienteBtn");
    const limparClienteBtn = document.getElementById("limparClienteBtn");
    const fecharModalCliente = document.getElementById("fecharModalCliente");
    const buscaClienteInput = document.getElementById("buscaClienteInput");
    const resultadosCliente = document.getElementById("resultadosCliente");

    const camposCliente = ["clienteNome", "clienteTelefone", "clienteDocumento", "clienteCep",
                           "clienteEndereco", "clienteNumero", "clienteBairro", "clienteCidade", "clienteUf"];

    function abrirModalCliente() {
        modalCliente.classList.remove("hidden");
        buscaClienteInput.value = "";
        buscaClienteInput.focus();
        procurarClientes("");
    }

    function fecharModal() {
        modalCliente.classList.add("hidden");
    }

    function procurarClientes(termo) {
        resultadosCliente.innerHTML = '<p class="px-md py-lg text-center font-body-md text-body-md text-on-surface-variant">Procurando...</p>';

        fetch("/Cliente/Buscar?termo=" + encodeURIComponent(termo))
            .then(function (r) { return r.json(); })
            .then(function (clientes) {
                resultadosCliente.innerHTML = "";

                if (clientes.length === 0) {
                    resultadosCliente.innerHTML =
                        '<p class="px-md py-lg text-center font-body-md text-body-md text-on-surface-variant">Nenhum cliente encontrado.</p>';
                    return;
                }

                clientes.forEach(function (c) {
                    const item = document.createElement("button");
                    item.type = "button";
                    item.className = "w-full text-left px-md py-sm border-b border-outline-variant hover:bg-surface-container-low transition-colors";

                    const nome = document.createElement("p");
                    nome.className = "font-body-md text-body-md text-on-surface";
                    nome.textContent = c.nome;

                    const detalhe = document.createElement("p");
                    detalhe.className = "font-label-sm text-label-sm text-outline";
                    detalhe.textContent = [c.telefone, c.documento, c.cidade].filter(Boolean).join(" · ") || "sem outros dados";

                    item.append(nome, detalhe);
                    item.addEventListener("click", function () { selecionarCliente(c); });
                    resultadosCliente.appendChild(item);
                });
            })
            .catch(function () {
                resultadosCliente.innerHTML =
                    '<p class="px-md py-lg text-center font-body-md text-body-md text-error">Não foi possível buscar os clientes.</p>';
            });
    }

    function selecionarCliente(c) {
        document.getElementById("clienteId").value = c.id;
        document.getElementById("clienteNome").value = c.nome;
        document.getElementById("clienteTelefone").value = c.telefone;
        document.getElementById("clienteDocumento").value = c.documento;
        document.getElementById("clienteCep").value = c.cep;
        document.getElementById("clienteEndereco").value = c.endereco;
        document.getElementById("clienteNumero").value = c.numero;
        document.getElementById("clienteBairro").value = c.bairro;
        document.getElementById("clienteCidade").value = c.cidade;
        document.getElementById("clienteUf").value = c.uf;

        buscarClienteBtn.classList.add("hidden");
        limparClienteBtn.classList.remove("hidden");
        fecharModal();
    }

    function limparCliente() {
        document.getElementById("clienteId").value = "";
        camposCliente.forEach(function (id) { document.getElementById(id).value = ""; });
        limparClienteBtn.classList.add("hidden");
        buscarClienteBtn.classList.remove("hidden");
    }

    buscarClienteBtn.addEventListener("click", abrirModalCliente);
    limparClienteBtn.addEventListener("click", limparCliente);
    fecharModalCliente.addEventListener("click", fecharModal);
    // O campo é somente leitura: clicar nele também abre a busca
    document.getElementById("clienteNome").addEventListener("click", function () {
        if (limparClienteBtn.classList.contains("hidden")) abrirModalCliente();
    });
    modalCliente.addEventListener("click", function (e) { if (e.target === modalCliente) fecharModal(); });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && !modalCliente.classList.contains("hidden")) fecharModal();
    });

    let buscaTimer;
    buscaClienteInput.addEventListener("input", function () {
        window.clearTimeout(buscaTimer);
        buscaTimer = window.setTimeout(function () { procurarClientes(buscaClienteInput.value); }, 250);
    });

    document.getElementById("formOs").addEventListener("submit", function (e) {
        if (!document.getElementById("clienteId").value) {
            e.preventDefault();
            mostrarToast("Selecione o cliente antes de salvar.", true);
            buscarClienteBtn.focus();
            return;
        }
        const temItem = Array.prototype.some.call(itensBody.querySelectorAll("tr"), function (tr) {
            return tr.querySelector(".item-desc").value.trim() || parseDecimal(tr.querySelector(".item-valor").value) > 0;
        });
        if (!temItem) {
            e.preventDefault();
            mostrarToast("Adicione ao menos um item ao orçamento.", true);
        }
    });


    // ===== Pagamento: haver, desconto e parcelamento =====

    const temSinal = document.getElementById("temSinal");
    const sinalVisivel = document.getElementById("sinalVisivel");
    const sinalPost = document.getElementById("sinal");
    const descontoVisivel = document.getElementById("descontoVisivel");
    const descontoPost = document.getElementById("desconto");
    const descontoTipo = document.getElementById("descontoTipo");
    const parcelado = document.getElementById("parcelado");
    const parcelas = document.getElementById("parcelas");
    const valorParcela = document.getElementById("valorParcela");

    function recalcularPagamento() {
        const subtotal = somaItens();
        const descontoBruto = parseDecimal(descontoVisivel.value);
        const descontoReais = descontoTipo.value === "valor"
            ? Math.min(descontoBruto, subtotal)
            : subtotal * (Math.min(descontoBruto, 100) / 100);

        const total = subtotal - descontoReais;
        const sinal = temSinal.checked ? parseDecimal(sinalVisivel.value) : 0;
        const saldo = Math.max(total - sinal, 0);

        document.getElementById("resumoSubtotal").textContent = formatBRL(subtotal);
        document.getElementById("resumoDesconto").textContent = "− " + formatBRL(descontoReais);
        document.getElementById("resumoSinal").textContent = "− " + formatBRL(sinal);
        document.getElementById("resumoSaldo").textContent = formatBRL(saldo);

        sinalVisivel.readOnly = !temSinal.checked;
        sinalVisivel.classList.toggle("opacity-50", !temSinal.checked);
        parcelas.disabled = !parcelado.checked;

        const n = parcelado.checked ? parseInt(parcelas.value, 10) || 1 : 1;
        valorParcela.textContent = n > 1 ? n + "x de " + formatBRL(saldo / n) : "À vista";

        // Sempre com ponto decimal para o servidor
        sinalPost.value = sinal.toFixed(2);
        descontoPost.value = descontoBruto.toFixed(2);
    }

    function somaItens() {
        let total = 0;
        itensBody.querySelectorAll("tr").forEach(function (tr) {
            const qtd = parseInt(tr.querySelector(".item-qtd").value, 10) || 0;
            total += qtd * parseDecimal(tr.querySelector(".item-valor").value);
        });
        return total;
    }

    [temSinal, sinalVisivel, descontoVisivel, descontoTipo, parcelado, parcelas].forEach(function (el) {
        el.addEventListener("input", recalcularPagamento);
        el.addEventListener("change", recalcularPagamento);
    });
    itensBody.addEventListener("input", recalcularPagamento);
    itensBody.addEventListener("click", recalcularPagamento);
    recalcularPagamento();


    // ===== Catálogo de serviços: preenche o item em vez de digitar =====

    const modalServico = document.getElementById("modalServico");
    const buscaServicoInput = document.getElementById("buscaServicoInput");
    const resultadosServico = document.getElementById("resultadosServico");
    const fecharModalServico = document.getElementById("fecharModalServico");
    let linhaDestino = null;

    function abrirCatalogo(tr) {
        linhaDestino = tr;
        modalServico.classList.remove("hidden");
        buscaServicoInput.value = "";
        buscaServicoInput.focus();
        procurarServicos("");
    }

    function fecharCatalogo() {
        modalServico.classList.add("hidden");
        linhaDestino = null;
    }

    function procurarServicos(termo) {
        resultadosServico.innerHTML = '<p class="px-md py-lg text-center font-body-md text-body-md text-on-surface-variant">Procurando...</p>';

        fetch("/Servico/Buscar?termo=" + encodeURIComponent(termo))
            .then(function (r) { return r.json(); })
            .then(function (servicos) {
                resultadosServico.innerHTML = "";

                if (servicos.length === 0) {
                    resultadosServico.innerHTML =
                        '<p class="px-md py-lg text-center font-body-md text-body-md text-on-surface-variant">Nenhum serviço no catálogo. Cadastre em Serviços ou digite o item à mão.</p>';
                    return;
                }

                servicos.forEach(function (s) {
                    const item = document.createElement("button");
                    item.type = "button";
                    item.className = "w-full text-left px-md py-sm border-b border-outline-variant hover:bg-surface-container-low transition-colors flex items-center justify-between gap-md";

                    const esquerda = document.createElement("div");
                    const nome = document.createElement("p");
                    nome.className = "font-body-md text-body-md text-on-surface";
                    nome.textContent = s.nome;
                    const detalhe = document.createElement("p");
                    detalhe.className = "font-label-sm text-label-sm text-outline";
                    detalhe.textContent = [s.categoria, s.descricao].filter(Boolean).join(" · ");
                    esquerda.append(nome, detalhe);

                    const preco = document.createElement("span");
                    preco.className = "font-body-md text-body-md font-semibold text-secondary shrink-0";
                    preco.textContent = formatBRL(s.valor);

                    item.append(esquerda, preco);
                    item.addEventListener("click", function () { aplicarServico(s); });
                    resultadosServico.appendChild(item);
                });
            })
            .catch(function () {
                resultadosServico.innerHTML =
                    '<p class="px-md py-lg text-center font-body-md text-body-md text-error">Não foi possível buscar o catálogo.</p>';
            });
    }

    function aplicarServico(s) {
        if (!linhaDestino) return;
        linhaDestino.querySelector(".item-desc").value = s.nome;
        // Valor do catálogo é sugestão: fica editável na linha
        linhaDestino.querySelector(".item-valor").value = Number(s.valor).toFixed(2).replace(".", ",");
        recalcular();
        recalcularPagamento();
        fecharCatalogo();
    }

    itensBody.addEventListener("click", function (e) {
        const botao = e.target.closest(".escolher-servico");
        if (botao) abrirCatalogo(botao.closest("tr"));
    });

    fecharModalServico.addEventListener("click", fecharCatalogo);
    modalServico.addEventListener("click", function (e) { if (e.target === modalServico) fecharCatalogo(); });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && !modalServico.classList.contains("hidden")) fecharCatalogo();
    });

    let buscaServicoTimer;
    buscaServicoInput.addEventListener("input", function () {
        window.clearTimeout(buscaServicoTimer);
        buscaServicoTimer = window.setTimeout(function () { procurarServicos(buscaServicoInput.value); }, 250);
    });

    // Ao editar, o cliente já vem escolhido: o botão vira o de trocar
    if (document.getElementById("clienteId").value) {
        buscarClienteBtn.classList.add("hidden");
        limparClienteBtn.classList.remove("hidden");
    }

    // Ao editar, os itens já vieram do servidor; só a OS nova começa com linha em branco
    if (itensBody.children.length === 0) adicionarItem(false);
    atualizarVazio();
    recalcular();
});
