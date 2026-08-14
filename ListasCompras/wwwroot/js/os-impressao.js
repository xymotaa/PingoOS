document.addEventListener("DOMContentLoaded", function () {
    // Área útil da A4 com as margens de 8mm definidas em @page, convertida para px (96dpi)
    const A4_LARGURA_UTIL = 194 * 96 / 25.4;
    const A4_ALTURA_UTIL = 281 * 96 / 25.4;
    const ESCALA_MINIMA = 0.75;

    function gerarSegundaVia() {
        const via1 = document.getElementById("osVia1");
        const via2 = document.getElementById("osVia2");
        // O orçamento sai em via única e não tem o segundo bloco
        if (!via1 || !via2) return;
        const copia = via1.cloneNode(true);
        copia.removeAttribute("id");
        copia.querySelectorAll("[id]").forEach(function (el) { el.removeAttribute("id"); });
        const rotulo = copia.querySelector(".os-via-label");
        if (rotulo) rotulo.textContent = "2ª via — Técnico";
        via2.innerHTML = "";
        via2.appendChild(copia);
    }

    function ajustarEscala() {
        const doc = document.getElementById("osImpressao");
        doc.style.zoom = "";
        // o documento fica oculto na tela: exibe fora da área visível só para medir
        const estiloOriginal = doc.getAttribute("style") || "";
        doc.style.cssText = estiloOriginal + ";display:block;position:absolute;visibility:hidden;left:-10000px;top:0;width:" + A4_LARGURA_UTIL + "px;";
        const altura = doc.getBoundingClientRect().height;
        doc.setAttribute("style", estiloOriginal);
        if (altura > A4_ALTURA_UTIL) {
            doc.style.zoom = Math.max(ESCALA_MINIMA, A4_ALTURA_UTIL / altura).toFixed(3);
        }
    }

    // O documento já vem pronto do servidor; só falta a 2ª via e o encaixe na folha
    gerarSegundaVia();
    ajustarEscala();

    const imprimirBtn = document.getElementById("imprimirBtn");
    if (imprimirBtn) imprimirBtn.addEventListener("click", function () { window.print(); });

    // Térmica e A4 nunca saem juntas: a classe no body escolhe qual das duas o CSS mostra
    const imprimirTermicoBtn = document.getElementById("imprimirTermicoBtn");
    if (imprimirTermicoBtn) {
        imprimirTermicoBtn.addEventListener("click", function () {
            document.body.classList.add("modo-termico");
            window.print();
        });
        window.addEventListener("afterprint", function () {
            document.body.classList.remove("modo-termico");
        });
    }
});
