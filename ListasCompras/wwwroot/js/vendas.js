document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll(".linha-venda[data-href]").forEach(function (linha) {
        linha.addEventListener("click", function () { window.location.href = linha.dataset.href; });
    });
});
