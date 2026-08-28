document.addEventListener("DOMContentLoaded", function () {
    const modal = document.getElementById("modalNotificar");
    const transicaoModal = UiTransicoes.modal(modal);
    const abrirBtn = document.getElementById("abrirNotificarBtn");
    const fecharBtn = document.getElementById("fecharModalNotificar");
    const cancelarBtn = document.getElementById("cancelarNotificarBtn");
    const form = document.getElementById("formNotificar");
    if (!modal || !abrirBtn) return;

    function abrir() { transicaoModal.abrir(); }
    function fechar() { transicaoModal.fechar(); }

    abrirBtn.addEventListener("click", abrir);
    fecharBtn.addEventListener("click", fechar);
    cancelarBtn.addEventListener("click", fechar);
    modal.addEventListener("click", function (e) { if (e.target === modal) fechar(); });
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && !modal.classList.contains("hidden")) fechar();
    });

    // O wa.me abre em nova aba (target="_blank"); esta aba só precisa recarregar para
    // mostrar o aviso recém-registrado na lista de histórico.
    form.addEventListener("submit", function () {
        fechar();
        setTimeout(function () { location.reload(); }, 300);
    });
});
