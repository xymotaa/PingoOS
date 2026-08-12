document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("formServico");
    const visivel = document.getElementById("valorVisivel");
    const post = document.getElementById("valor");

    function parseDecimal(v) {
        return parseFloat(String(v).replace(/\./g, "").replace(",", ".")) || 0;
    }

    // O campo aceita vírgula; o servidor recebe ponto, senão a cultura do sistema
    // converteria "620,00" errado
    form.addEventListener("submit", function () {
        post.value = parseDecimal(visivel.value).toFixed(2);
    });
});
