// Modal "nova categoria" compartilhado entre Estoque, Pedidos e a tela de Categorias.
// Espera: #modalNovaCategoria, #novaCategoriaNome, #salvarNovaCategoriaBtn,
// #fecharNovaCategoriaBtn, #novaCategoriaErro, um botão que abre o modal com
// [data-abre-nova-categoria], e a constante global CATEGORIA_CRIAR_URL.
// Na tela de Categorias (Categoria/Index), clicar numa linha [data-id] abre o mesmo
// modal em modo edição (usa #formEditarCategoria, um submit de página normal em vez
// de fetch, porque editar/excluir já recarrega a lista mesmo).
document.addEventListener("DOMContentLoaded", function () {
    var modal = document.getElementById("modalNovaCategoria");
    if (!modal) return;

    var nomeInput = document.getElementById("novaCategoriaNome");
    var requerModeloInput = document.getElementById("novaCategoriaRequerModelo");
    var erroEl = document.getElementById("novaCategoriaErro");
    var salvarBtn = document.getElementById("salvarNovaCategoriaBtn");
    var fecharBtn = document.getElementById("fecharNovaCategoriaBtn");
    var tituloEl = document.getElementById("modalCategoriaTitulo");
    var abrirBtns = Array.prototype.slice.call(document.querySelectorAll("[data-abre-nova-categoria]"));
    var linhasCategoria = Array.prototype.slice.call(document.querySelectorAll(".linha-categoria[data-id]"));
    var formEditar = document.getElementById("formEditarCategoria");

    var editandoId = null;
    var transicaoModal = UiTransicoes.modal(modal);

    function tokenAntiForgery() {
        var el = document.querySelector('#formNovaCategoria input[name="__RequestVerificationToken"]');
        return el ? el.value : "";
    }

    function abrir() {
        editandoId = null;
        if (tituloEl) tituloEl.textContent = "Nova categoria";
        if (salvarBtn) salvarBtn.textContent = "Criar categoria";
        erroEl.classList.add("hidden");
        nomeInput.value = "";
        if (requerModeloInput) requerModeloInput.checked = false;
        transicaoModal.abrir();
        nomeInput.focus();
    }

    function abrirParaEditar(linha) {
        editandoId = linha.dataset.id;
        if (tituloEl) tituloEl.textContent = "Editar categoria";
        if (salvarBtn) salvarBtn.textContent = "Salvar";
        erroEl.classList.add("hidden");
        nomeInput.value = linha.dataset.nome || "";
        if (requerModeloInput) requerModeloInput.checked = linha.dataset.requerModelo === "true";
        transicaoModal.abrir();
        nomeInput.focus();
    }

    function fechar() { transicaoModal.fechar(); }

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

    function salvarEdicao(nome) {
        if (!formEditar) return;
        document.getElementById("editarCategoriaId").value = editandoId;
        document.getElementById("editarCategoriaNome").value = nome;
        document.getElementById("editarCategoriaRequerModelo").value = requerModeloInput && requerModeloInput.checked ? "true" : "false";
        formEditar.submit();
    }

    function salvar() {
        var nome = nomeInput.value.trim();
        if (!nome) {
            erroEl.textContent = "Informe o nome da categoria.";
            erroEl.classList.remove("hidden");
            return;
        }

        if (editandoId) {
            salvarEdicao(nome);
            return;
        }

        salvarBtn.disabled = true;
        fetch(CATEGORIA_CRIAR_URL, {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "RequestVerificationToken": tokenAntiForgery(),
            },
            body: "nome=" + encodeURIComponent(nome)
                + "&requerModelo=" + (requerModeloInput && requerModeloInput.checked ? "true" : "false"),
        })
            .then(function (resp) { return resp.json().then(function (data) { return { ok: resp.ok, data: data }; }); })
            .then(function (r) {
                salvarBtn.disabled = false;
                if (!r.ok) {
                    erroEl.textContent = r.data.erro || "Não foi possível criar a categoria.";
                    erroEl.classList.remove("hidden");
                    return;
                }
                var temSelect = document.querySelector("[data-categoria-select]") !== null;
                if (temSelect) {
                    adicionarNosSelects(r.data.id, r.data.nome);
                    fechar();
                } else {
                    // Tela de Categorias: sem select pra atualizar, recarrega pra mostrar a linha nova
                    window.location.reload();
                }
            })
            .catch(function () {
                salvarBtn.disabled = false;
                erroEl.textContent = "Falha de conexão. Tente novamente.";
                erroEl.classList.remove("hidden");
            });
    }

    abrirBtns.forEach(function (btn) { btn.addEventListener("click", abrir); });
    linhasCategoria.forEach(function (linha) {
        linha.addEventListener("click", function () { abrirParaEditar(linha); });
    });
    if (fecharBtn) fecharBtn.addEventListener("click", fechar);
    if (salvarBtn) salvarBtn.addEventListener("click", salvar);
    modal.addEventListener("click", function (e) { if (e.target === modal) fechar(); });
    nomeInput.addEventListener("keydown", function (e) { if (e.key === "Enter") { e.preventDefault(); salvar(); } });
});
