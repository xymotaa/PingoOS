document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("formProduto");
    const npNome = document.getElementById("npNome");
    const npPreco = document.getElementById("npPreco");
    const npCusto = document.getElementById("npCusto");
    const npPrecoPost = document.getElementById("npPrecoPost");
    const npCustoPost = document.getElementById("npCustoPost");

    // Etapas do cadastro
    const steps = Array.prototype.slice.call(document.querySelectorAll(".np-step"));
    const paineis = Array.prototype.slice.call(document.querySelectorAll("[data-step-panel]"));
    const avancarBtn = document.getElementById("npAvancarEtapaBtn");
    let etapaAtual = 1;

    function irParaEtapa(n) {
        etapaAtual = n;
        steps.forEach(function (s, i) {
            const ativo = i === n - 1;
            s.classList.toggle("border-secondary", ativo);
            s.classList.toggle("text-secondary", ativo);
            s.classList.toggle("border-transparent", !ativo);
            s.classList.toggle("text-on-surface-variant", !ativo);
        });
        paineis.forEach(function (p, i) { p.classList.toggle("hidden", i !== n - 1); });
        // Na última etapa não tem para onde avançar
        if (avancarBtn) avancarBtn.classList.toggle("hidden", n >= steps.length);
    }

    steps.forEach(function (s, i) {
        s.addEventListener("click", function () { irParaEtapa(i + 1); });
    });

    if (avancarBtn) {
        avancarBtn.addEventListener("click", function () {
            if (etapaAtual < steps.length) irParaEtapa(etapaAtual + 1);
        });
    }

    function parseDecimal(valor) {
        return parseFloat(String(valor).replace(/\./g, "").replace(",", ".")) || 0;
    }

    // Preview da imagem do produto (etapa 3) — o arquivo só é enviado no submit do
    // formulário inteiro, igual ao resto do cadastro (não é envio imediato).
    const npFotoInput = document.getElementById("npFotoInput");
    const npFotoPreview = document.getElementById("npFotoPreview");
    const npFotoPlaceholder = document.getElementById("npFotoPlaceholder");
    const npFotoRemoverBtn = document.getElementById("npFotoRemoverBtn");
    const npRemoverFotoInput = document.getElementById("npRemoverFotoInput");

    if (npFotoInput) {
        npFotoInput.addEventListener("change", function () {
            const file = this.files && this.files[0];
            if (!file) return;
            const reader = new FileReader();
            reader.onload = function (e) {
                npFotoPreview.src = e.target.result;
                npFotoPreview.classList.remove("hidden");
                npFotoPlaceholder.classList.add("hidden");
                npFotoRemoverBtn.classList.remove("hidden");
                npRemoverFotoInput.value = "false"; // escolher um arquivo novo cancela um "remover" pendente
            };
            reader.readAsDataURL(file);
        });

        npFotoRemoverBtn.addEventListener("click", function () {
            npFotoInput.value = "";
            npFotoPreview.src = "";
            npFotoPreview.classList.add("hidden");
            npFotoPlaceholder.classList.remove("hidden");
            npFotoRemoverBtn.classList.add("hidden");
            npRemoverFotoInput.value = "true";
        });
    }

    // ===== Formato: simples vs. com variação =====
    const npFormato = document.getElementById("npFormato");
    const npEstoqueSimplesWrap = document.getElementById("npEstoqueSimplesWrap");
    const npEstoqueVariacaoAviso = document.getElementById("npEstoqueVariacaoAviso");
    const npVariacoesAviso = document.getElementById("npVariacoesAviso");
    const npVariacoesWrap = document.getElementById("npVariacoesWrap");
    const npVariacoesBody = document.getElementById("npVariacoesBody");
    const npAddVariacaoBtn = document.getElementById("npAddVariacaoBtn");
    const npEstoqueInicialInput = document.getElementById("npEstoqueInicial");
    const npEstoqueMinimoInput = document.getElementById("npEstoqueMinimo");
    const npEstoqueMaximoInput = document.getElementById("npEstoqueMaximo");

    const variacoesIniciais = typeof VARIACOES_EXISTENTES !== "undefined" ? VARIACOES_EXISTENTES : [];
    let variacaoSeq = 0;

    function escapeAtributo(v) {
        return String(v == null ? "" : v).replace(/"/g, "&quot;");
    }

    function linhaVariacaoHtml(v) {
        v = v || {};
        variacaoSeq++;
        const precoStr = v.preco != null ? Number(v.preco).toFixed(2).replace(".", ",") : "";
        const temId = v.id ? v.id : 0;
        return '<tr>' +
            '<td><input type="hidden" name="variacaoId" value="' + temId + '" />' +
                '<input type="text" name="variacaoDescricao" value="' + escapeAtributo(v.descricao) + '" maxlength="80" placeholder="Ex: Preta, 64GB" class="w-full bg-surface-container-low border-none rounded-lg px-sm py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" /></td>' +
            '<td><input type="text" name="variacaoCodigo" value="' + escapeAtributo(v.codigo) + '" maxlength="40" placeholder="Auto" class="w-24 bg-surface-container-low border-none rounded-lg px-sm py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" /></td>' +
            '<td><input type="text" name="variacaoPreco" value="' + escapeAtributo(precoStr) + '" inputmode="decimal" data-mascara="valor" placeholder="Herda do produto" class="w-24 bg-surface-container-low border-none rounded-lg px-sm py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" /></td>' +
            '<td><input type="number" name="variacaoEstoqueAtual" value="' + (v.estoqueAtual || 0) + '" min="0" class="w-20 bg-surface-container-low border-none rounded-lg px-sm py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" /></td>' +
            '<td><input type="number" name="variacaoEstoqueMinimo" value="' + (v.estoqueMinimo || "") + '" min="0" class="w-20 bg-surface-container-low border-none rounded-lg px-sm py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" /></td>' +
            '<td><input type="number" name="variacaoEstoqueMaximo" value="' + (v.estoqueMaximo || "") + '" min="0" class="w-20 bg-surface-container-low border-none rounded-lg px-sm py-2 font-body-md text-body-md focus:ring-2 focus:ring-secondary/30" /></td>' +
            '<td>' + (temId
                ? '<button type="button" class="npExcluirVariacaoBtn text-error" data-id="' + temId + '" title="Excluir variação"><span class="material-symbols-outlined text-[18px]">delete</span></button>'
                : '<button type="button" class="npRemoverLinhaVariacaoBtn text-error" title="Remover linha"><span class="material-symbols-outlined text-[18px]">close</span></button>') +
            '</td></tr>';
    }

    function adicionarLinhaVariacao(v) {
        if (npVariacoesBody) npVariacoesBody.insertAdjacentHTML("beforeend", linhaVariacaoHtml(v));
    }

    if (npAddVariacaoBtn) {
        npAddVariacaoBtn.addEventListener("click", function () { adicionarLinhaVariacao(); });
    }

    if (npVariacoesBody) {
        npVariacoesBody.addEventListener("click", function (e) {
            const removerLinha = e.target.closest(".npRemoverLinhaVariacaoBtn");
            if (removerLinha) { removerLinha.closest("tr").remove(); return; }

            const excluirBtn = e.target.closest(".npExcluirVariacaoBtn");
            if (excluirBtn) {
                if (!confirm("Excluir esta variação? Só é possível se o saldo dela estiver zerado.")) return;
                const idVariacao = excluirBtn.dataset.id;
                const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
                const fd = new FormData();
                fd.append("id", idVariacao);
                if (tokenInput) fd.append("__RequestVerificationToken", tokenInput.value);
                fetch(EXCLUIR_VARIACAO_URL, { method: "POST", body: fd })
                    .then(function () { location.reload(); });
            }
        });
    }

    function atualizarVisibilidadePorFormato() {
        if (!npFormato) return;
        const comVariacao = npFormato.value === "variacao";
        if (npEstoqueSimplesWrap) npEstoqueSimplesWrap.classList.toggle("hidden", comVariacao);
        if (npEstoqueVariacaoAviso) npEstoqueVariacaoAviso.classList.toggle("hidden", !comVariacao);
        if (npVariacoesAviso) npVariacoesAviso.classList.toggle("hidden", comVariacao);
        if (npVariacoesWrap) npVariacoesWrap.classList.toggle("hidden", !comVariacao);

        // Campo disabled não é enviado no submit — evita mandar um valor antigo
        // escondido que reative um saldo indevido no servidor para o pai.
        if (npEstoqueInicialInput) npEstoqueInicialInput.disabled = comVariacao;
        if (npEstoqueMinimoInput) npEstoqueMinimoInput.disabled = comVariacao;
        if (npEstoqueMaximoInput) npEstoqueMaximoInput.disabled = comVariacao;
    }

    if (npFormato) {
        npFormato.addEventListener("change", atualizarVisibilidadePorFormato);

        variacoesIniciais.forEach(function (v) { adicionarLinhaVariacao(v); });
        if (variacoesIniciais.length === 0 && npFormato.value === "variacao") adicionarLinhaVariacao();
        atualizarVisibilidadePorFormato();
    }

    // O visível aceita vírgula; o que vai ao servidor usa ponto, senão a cultura
    // do sistema converteria "620,00" errado
    form.addEventListener("submit", function (e) {
        if (!npNome.value.trim()) {
            e.preventDefault();
            irParaEtapa(1);
            npNome.focus();
            return;
        }
        npPrecoPost.value = parseDecimal(npPreco.value).toFixed(2);
        npCustoPost.value = parseDecimal(npCusto.value).toFixed(2);

        // location.replace troca a página atual no histórico em vez de empilhar: o
        // formulário some do histórico e "voltar" na tela seguinte cai na listagem.
        e.preventDefault();
        fetch(form.action, { method: "POST", body: new FormData(form) })
            .then(function (r) { location.replace(r.url); })
            .catch(function () { form.submit(); });
    });

    irParaEtapa(1);
});
