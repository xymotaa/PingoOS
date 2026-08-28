// Abrir/fechar modal com fade do fundo + fade/leve escala do card, em vez do hidden/visible
// abrupto usado antes em todo o sistema. Helper único pra não duplicar a mesma lógica em cada
// tela — qualquer modal no padrão `<div id="X" class="hidden fixed inset-0 ..."><div>card</div></div>`
// funciona sem marcação extra: as classes de transição são aplicadas aqui, via JS, não escritas
// à mão em cada .cshtml.
//
// Uso: UiTransicoes.modal(document.getElementById("meuModal")).abrir() / .fechar()
// O card é sempre o primeiro elemento filho do modal.
const UiTransicoes = (function () {
    const DURACAO_MS = 150;

    function modal(elemento) {
        if (!elemento) return { abrir() {}, fechar() {} };

        const card = elemento.firstElementChild;

        // Prepara as classes de transição uma única vez (idempotente — chamar de novo não duplica).
        if (!elemento.dataset.uiTransicaoPronta) {
            elemento.classList.add("transition-opacity", "duration-150", "ease-out");
            if (card) card.classList.add("transition-all", "duration-150", "ease-out");
            elemento.dataset.uiTransicaoPronta = "1";
        }

        function estadoFechado() {
            elemento.classList.add("opacity-0");
            if (card) card.classList.add("opacity-0", "scale-95");
        }
        function estadoAberto() {
            elemento.classList.remove("opacity-0");
            if (card) card.classList.remove("opacity-0", "scale-95");
        }

        return {
            abrir() {
                estadoFechado();
                elemento.classList.remove("hidden");
                // Força o navegador a pintar o estado fechado antes de tirar as classes —
                // sem isso as duas mudanças colapsam num único frame e não anima nada.
                requestAnimationFrame(function () {
                    requestAnimationFrame(estadoAberto);
                });
            },
            fechar() {
                if (elemento.classList.contains("hidden")) return;
                estadoFechado();
                setTimeout(function () { elemento.classList.add("hidden"); }, DURACAO_MS);
            },
        };
    }

    return { modal };
})();
