// Modal "nova categoria" compartilhado entre Estoque e Pedidos.
// Espera: #modalNovaCategoria, #novaCategoriaNome, #salvarNovaCategoriaBtn,
// #fecharNovaCategoriaBtn, #novaCategoriaErro, um botão que abre o modal com
// [data-abre-nova-categoria], e a constante global CATEGORIA_CRIAR_URL.
document.addEventListener("DOMContentLoaded", function () {
    var modal = document.getElementById("modalNovaCategoria");
    if (!modal) return;

    var nomeInput = document.getElementById("novaCategoriaNome");
    var erroEl = document.getElementById("novaCategoriaErro");
    var salvarBtn = document.getElementById("salvarNovaCategoriaBtn");
    var fecharBtn = document.getElementById("fecharNovaCategoriaBtn");
    var abrirBtns = Array.prototype.slice.call(document.querySelectorAll("[data-abre-nova-categoria]"));

    function tokenAntiForgery() {
        var el = document.querySelector('#formNovaCategoria input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    function abrir() {
        erroEl.classList.add("hidden");
        nomeInput.value = "";
        modal.classList.remove("hidden");
        nomeInput.focus();
    }

    function fechar() { modal.classList.add("hidden"); }

    function adicionarNosSelects(id, nome) {
        document.querySelectorAll("[data-categoria-select]").forEach(function (select) {
            var option = document.createElement("option");
            option.value = id;
            option.textContent = nome;
            option.setAttribute("data-requer-modelo", "false");
            select.appendChild(option);
            select.value = id;
            select.dispatchEvent(new Event("change"));
        });
    }

    function salvar() {
        var nome = nomeInput.value.trim();
        if (!nome) {
            erroEl.textContent = "Informe o nome da categoria.";
            erroEl.classList.remove("hidden");
            return;
        }

        salvarBtn.disabled = true;
        fetch(CATEGORIA_CRIAR_URL, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "RequestVerificationToken": tokenAntiForgery(),
            },
            body: "nome=" + encodeURIComponent(nome),
        })
            .then(function (resp) { return resp.json().then(function (data) { return { ok: resp.ok, data: data }; }); })
            .then(function (r) {
                salvarBtn.disabled = false;
                if (!r.ok) {
                    erroEl.textContent = r.data.erro || "Não foi possível criar a categoria.";
                    erroEl.classList.remove("hidden");
                    return;
                }
                adicionarNosSelects(r.data.id, r.data.nome);
                fechar();
            })
            .catch(function () {
                salvarBtn.disabled = false;
                erroEl.textContent = "Falha de conexão. Tente novamente.";
                erroEl.classList.remove("hidden");
            });
    }

    abrirBtns.forEach(function (btn) { btn.addEventListener("click", abrir); });
    if (fecharBtn) fecharBtn.addEventListener("click", fechar);
    if (salvarBtn) salvarBtn.addEventListener("click", salvar);
    modal.addEventListener("click", function (e) { if (e.target === modal) fechar(); });
    nomeInput.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); salvar(); } });
});
