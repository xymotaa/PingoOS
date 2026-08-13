document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("formServico");
    const visivel = document.getElementById("valorVisivel");
    const post = document.getElementById("valor");

    function parseDecimal(v) {
        return parseFloat(String(v).replace(/\./g, "").replace(",", ".")) || 0;
    }

    // O campo aceita vírgula; o servidor recebe ponto, senão a cultura do sistema
    // converteria "620,00" errado
    form.addEventListener("submit", function (e) {
        post.value = parseDecimal(visivel.value).toFixed(2);

        // location.replace troca a página atual no histórico em vez de empilhar: o
        // formulário some do histórico e "voltar" na tela seguinte cai na listagem.
        e.preventDefault();
        fetch(form.action, { method: "POST", body: new FormData(form) })
            .then(function (r) { location.replace(r.url); })
            .catch(function () { form.submit(); });
    });
});
