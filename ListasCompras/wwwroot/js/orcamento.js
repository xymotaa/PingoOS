document.addEventListener("DOMContentLoaded", function () {
    const itensBody = document.getElementById("itensBody");
    const itensVazio = document.getElementById("itensVazio");
    const totalGeral = document.getElementById("totalGeral");
    const adicionarItemBtn = document.getElementById("adicionarItemBtn");
    const salvarOrcamentoBtn = document.getElementById("salvarOrcamentoBtn");
    const imprimirBtn = document.getElementById("imprimirBtn");
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
                '<input type="text" class="item-desc w-full bg-surface-container-low border-none rounded-lg px-md py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" placeholder="Ex: Tela Display Frontal Original" />' +
            '</td>' +
            '<td class="px-md py-sm">' +
                '<input type="text" inputmode="numeric" value="1" class="item-qtd w-full text-center bg-surface-container-low border-none rounded-lg px-2 py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" />' +
            '</td>' +
            '<td class="px-md py-sm">' +
                '<input type="text" inputmode="decimal" placeholder="0,00" class="item-valor w-full text-right bg-surface-container-low border-none rounded-lg px-md py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" />' +
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

    function textoOuTraco(valor) {
        valor = (valor || "").trim();
        return valor || "—";
    }

    function definirTexto(id, valor) {
        document.getElementById(id).textContent = valor;
    }

    function valor(id) {
        return document.getElementById(id).value.trim();
    }

    function montarEndereco() {
        const partes = [];
        const rua = valor("clienteEndereco");
        const num = valor("clienteNumero");
        const linha = rua + (num ? ", " + num : "");
        if (linha.trim()) partes.push(linha.trim());
        if (valor("clienteBairro")) partes.push(valor("clienteBairro"));
        const cidade = valor("clienteCidade");
        const uf = valor("clienteUf");
        const cidadeUf = cidade + (uf ? "/" + uf.toUpperCase() : "");
        if (cidadeUf.trim()) partes.push(cidadeUf.trim());
        if (valor("clienteCep")) partes.push("CEP " + valor("clienteCep"));
        return partes.length ? partes.join(" - ") : "—";
    }

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

    function prepararImpressao() {
        definirTexto("osNumero", "OS-" + Date.now().toString().slice(-6));
        definirTexto("osData", new Date().toLocaleDateString("pt-BR"));
        definirTexto("osClienteNome", textoOuTraco(document.getElementById("clienteNome").value));
        definirTexto("osClienteTelefone", textoOuTraco(document.getElementById("clienteTelefone").value));
        definirTexto("osClienteDoc", textoOuTraco(document.getElementById("clienteDocumento").value));
        definirTexto("osClienteEndereco", montarEndereco());
        definirTexto("osDispTipo", textoOuTraco(document.getElementById("dispositivoTipo").value));
        definirTexto("osDispMarca", textoOuTraco(document.getElementById("dispositivoMarca").value));
        definirTexto("osDispModelo", textoOuTraco(document.getElementById("dispositivoModelo").value));
        definirTexto("osDispSerie", dispositivoSemSerie.checked ? "Não possui" : textoOuTraco(document.getElementById("dispositivoSerie").value));
        definirTexto("osDiagnostico", textoOuTraco(document.getElementById("diagnostico").value));

        const osItens = document.getElementById("osItens");
        osItens.innerHTML = "";
        itensBody.querySelectorAll("tr").forEach(function (tr) {
            const desc = tr.querySelector(".item-desc").value.trim();
            const qtd = parseInt(tr.querySelector(".item-qtd").value, 10) || 0;
            const valor = parseDecimal(tr.querySelector(".item-valor").value);
            if (!desc && valor === 0) return; // ignora linhas vazias
            const linha = document.createElement("tr");
            const tdDesc = document.createElement("td");
            tdDesc.textContent = desc || "—";
            const tdQtd = document.createElement("td");
            tdQtd.style.textAlign = "center";
            tdQtd.textContent = qtd;
            const tdValor = document.createElement("td");
            tdValor.style.textAlign = "right";
            tdValor.textContent = formatBRL(valor);
            const tdTotal = document.createElement("td");
            tdTotal.style.textAlign = "right";
            tdTotal.textContent = formatBRL(qtd * valor);
            linha.append(tdDesc, tdQtd, tdValor, tdTotal);
            osItens.appendChild(linha);
        });
        definirTexto("osTotal", totalGeral.textContent);
        gerarSegundaVia();
        ajustarEscala();
    }

    // Área útil da A4 com as margens de 8mm definidas em @page, convertida para px (96dpi)
    const A4_LARGURA_UTIL = 194 * 96 / 25.4;
    const A4_ALTURA_UTIL = 281 * 96 / 25.4;
    const ESCALA_MINIMA = 0.75;

    // Com muitos itens as duas vias estouram a folha; reduz a escala o suficiente para caberem
    function ajustarEscala() {
        const doc = document.getElementById("osImpressao");
        doc.style.zoom = "";
        // o documento fica oculto na tela: exibe fora da área visível só para medir
        const estiloOriginal = doc.getAttribute("style") || "";
        doc.style.cssText = estiloOriginal + ";display:block;position:absolute;visibility:hidden;left:-10000px;top:0;width:" + A4_LARGURA_UTIL + "px;";
        const altura = doc.getBoundingClientRect().height;
        doc.setAttribute("style", estiloOriginal);
        if (altura > A4_ALTURA_UTIL) {
            doc.style.zoom = Math.max(ESCALA_MINIMA, A4_ALTURA_UTIL / altura).toFixed(3);
        }
    }

    // Duplica a 1ª via na mesma folha para que cliente e técnico assinem cada uma a sua
    function gerarSegundaVia() {
        const via1 = document.getElementById("osVia1");
        const via2 = document.getElementById("osVia2");
        const copia = via1.cloneNode(true);
        copia.removeAttribute("id");
        copia.querySelectorAll("[id]").forEach(function (el) { el.removeAttribute("id"); });
        const rotulo = copia.querySelector(".os-via-label");
        if (rotulo) rotulo.textContent = "2ª via — Técnico";
        via2.innerHTML = "";
        via2.appendChild(copia);
    }

    imprimirBtn.addEventListener("click", function () {
        prepararImpressao();
        window.print();
    });

    salvarOrcamentoBtn.addEventListener("click", function () {
        const nome = document.getElementById("clienteNome").value.trim();
        if (!nome) {
            document.getElementById("clienteNome").focus();
            mostrarToast("Informe o nome do cliente para salvar o orçamento.", true);
            return;
        }
        mostrarToast("Orçamento salvo (exemplo, ainda não gravado no banco de dados).");
    });

    // Começa com uma linha em branco pronta para preencher
    adicionarItem(false);
});
