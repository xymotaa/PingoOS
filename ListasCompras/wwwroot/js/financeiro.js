document.addEventListener("DOMContentLoaded", function () {
    function parseDecimal(v) {
        return parseFloat(String(v).replace(/\./g, "").replace(",", ".")) || 0;
    }

    // O campo aceita vírgula; o servidor recebe ponto, senão a cultura do sistema
    // converteria "620,00" errado
    function ligar(visivelId, postId) {
        var visivel = document.getElementById(visivelId);
        var post = document.getElementById(postId);
        if (!visivel || !post) return;
        visivel.closest("form").addEventListener("submit", function () {
            post.value = parseDecimal(visivel.value).toFixed(2);
        });
    }

    ligar("valorVisivel", "valor");
    ligar("valorContaVisivel", "valorConta");
});
