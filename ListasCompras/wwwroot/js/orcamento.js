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
