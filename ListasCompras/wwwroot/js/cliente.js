document.addEventListener("DOMContentLoaded", function () {
    const filtroBusca = document.getElementById("filtroBusca");
    const linhas = Array.prototype.slice.call(document.querySelectorAll(".linha-cliente"));
    const vazioState = document.getElementById("vazioState");
    const contagemInfo = document.getElementById("contagemInfo");
    const imprimirBtn = document.getElementById("imprimirBtn");

    function atualizarContagem(visiveis) {
        if (linhas.length === 0) {
            contagemInfo.textContent = "";
            return;
        }
        contagemInfo.textContent = visiveis === linhas.length
            ? linhas.length + (linhas.length === 1 ? " cliente" : " clientes")
            : visiveis + " de " + linhas.length + " clientes";
    }

    function filtrar() {
        const termo = filtroBusca.value.trim().toLowerCase();
        let visiveis = 0;

        linhas.forEach(function (linha) {
            const combina = !termo || linha.dataset.busca.indexOf(termo) !== -1;
            linha.classList.toggle("hidden", !combina);
            if (combina) visiveis++;
        });

        // O estado vazio só entra em cena quando existem clientes mas nenhum casa com a busca
        if (linhas.length > 0) vazioState.classList.toggle("hidden", visiveis > 0);
        atualizarContagem(visiveis);
    }

    if (filtroBusca) filtroBusca.addEventListener("input", filtrar);
    if (imprimirBtn) imprimirBtn.addEventListener("click", function () { window.print(); });

    atualizarContagem(linhas.length);
});
