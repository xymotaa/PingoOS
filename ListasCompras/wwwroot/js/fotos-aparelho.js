document.addEventListener("DOMContentLoaded", function () {
    // Envia por fetch e troca a página com location.replace: recarrega a lista de fotos
    // sem empilhar o próprio submit no histórico do navegador.
    function enviarESubstituir(form) {
        fetch(form.action, { method: "POST", body: new FormData(form) })
            .then(function (r) { location.replace(r.url); })
            .catch(function () { form.submit(); });
    }

    document.querySelectorAll(".form-enviar-foto").forEach(function (form) {
        form.querySelector(".input-foto").addEventListener("change", function () {
            if (this.files.length) enviarESubstituir(form);
        });
    });

    document.querySelectorAll(".form-excluir-foto").forEach(function (form) {
        form.addEventListener("submit", function (e) {
            if (!confirm("Remover esta foto?")) { e.preventDefault(); return; }
            e.preventDefault();
            enviarESubstituir(form);
        });
    });
});
