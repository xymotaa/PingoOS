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
                '<input type="text" name="itemDescricao" class="item-desc w-full bg-surface-container-low border-none rounded-lg px-md py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" placeholder="Ex: Tela Display Frontal Original" />' +
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

    // "Sem número de série / IMEI": desabilita o campo
    const dispositivoSemSerie = document.getElementById("dispositivoSemSerie");
    const dispositivoSerie = document.getElementById("dispositivoSerie");
    dispositivoSemSerie.addEventListener("change", function () {
        dispositivoSerie.disabled = dispositivoSemSerie.checked;
        if (dispositivoSemSerie.checked) dispositivoSerie.value = "";
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
