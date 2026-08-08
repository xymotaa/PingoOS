document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("formProduto");
    const npNome = document.getElementById("npNome");
    const npPreco = document.getElementById("npPreco");
    const npCusto = document.getElementById("npCusto");
    const npPrecoPost = document.getElementById("npPrecoPost");
    const npCustoPost = document.getElementById("npCustoPost");

    // Etapas do cadastro
    const steps = Array.prototype.slice.call(document.querySelectorAll(".np-step"));
    const paineis = Array.prototype.slice.call(document.querySelectorAll(".np-painel"));

    function irParaEtapa(n) {
        steps.forEach(function (s, i) {
            const ativo = i === n - 1;
            s.classList.toggle("border-secondary", ativo);
            s.classList.toggle("text-secondary", ativo);
            s.classList.toggle("border-transparent", !ativo);
            s.classList.toggle("text-on-surface-variant", !ativo);
        });
        paineis.forEach(function (p, i) { p.classList.toggle("hidden", i !== n - 1); });
    }

    steps.forEach(function (s, i) {
        s.addEventListener("click", function () { irParaEtapa(i + 1); });
    });

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
    });

    irParaEtapa(1);
});
