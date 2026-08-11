document.addEventListener("DOMContentLoaded", function () {
    const filtroBusca = document.getElementById("filtroBusca");
    const filtroSituacao = document.getElementById("filtroSituacao");
    const linhas = Array.prototype.slice.call(document.querySelectorAll(".linha-os"));
    const vazioState = document.getElementById("vazioState");
    const contagemInfo = document.getElementById("contagemInfo");

    function atualizarContagem(visiveis) {
        if (linhas.length === 0) { contagemInfo.textContent = ""; return; }
        contagemInfo.textContent = visiveis === linhas.length
            ? linhas.length + (linhas.length === 1 ? " ordem" : " ordens")
            : visiveis + " de " + linhas.length + " ordens";
    }

    function filtrar() {
        const termo = filtroBusca.value.trim().toLowerCase();
        const situacao = filtroSituacao.value;
        let visiveis = 0;

        linhas.forEach(function (linha) {
            const combina = (!termo || linha.dataset.busca.indexOf(termo) !== -1)
                         && (!situacao || linha.dataset.situacao === situacao);
            linha.classList.toggle("hidden", !combina);
            if (combina) visiveis++;
        });

        if (linhas.length > 0) vazioState.classList.toggle("hidden", visiveis > 0);
        atualizarContagem(visiveis);
    }

    filtroBusca.addEventListener("input", filtrar);
    filtroSituacao.addEventListener("change", filtrar);
    atualizarContagem(linhas.length);
});

// Clicar na linha abre a ordem — menos quando o clique foi num controle dela
// (o seletor de situação, o botão de excluir, o ícone de ver)
function abrirOs(evento, url) {
    if (evento.target.closest("select, button, a, form, input")) return;
    window.location.href = url;
}
