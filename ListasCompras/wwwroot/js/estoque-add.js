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
