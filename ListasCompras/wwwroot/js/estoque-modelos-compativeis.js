document.addEventListener("DOMContentLoaded", function () {
    var buscaInput = document.getElementById("modelosCompativeisBusca");
    if (!buscaInput) return;

    var resultadosEl = document.getElementById("modelosCompativeisResultados");
    var chipsEl = document.getElementById("modelosCompativeisChips");
    var inputsEl = document.getElementById("modelosCompativeisInputs");
    var wrap = document.getElementById("modelosCompativeisWrap");

    var itensBusca = (typeof MARCAS_CELULAR !== "undefined" ? MARCAS_CELULAR : []).flatMap(function (marca) {
        return marca.modelos.map(function (modelo) {
            return { modeloId: modelo.id, modeloNome: modelo.nome, marcaNome: marca.nome };
        });
    });
    var selecionados = new Set(typeof MODELOS_COMPATIVEIS_SELECIONADOS !== "undefined" ? MODELOS_COMPATIVEIS_SELECIONADOS : []);

    function item(id) {
        return itensBusca.find(function (i) { return i.modeloId === id; });
    }

    function renderizarChips() {
        chipsEl.innerHTML = "";
        Array.from(selecionados).forEach(function (id) {
            var info = item(id);
            if (!info) return;
            var chip = document.createElement("span");
            chip.className = "inline-flex items-center gap-xs bg-surface-container-low rounded-full px-sm py-xs font-label-md text-label-md text-on-surface-variant";

            var texto = document.createElement("span");
            texto.textContent = info.marcaNome + " " + info.modeloNome;

            var remover = document.createElement("button");
            remover.type = "button";
            remover.dataset.remover = String(id);
            remover.className = "material-symbols-outlined text-[16px] text-outline hover:text-error";
            remover.textContent = "close";

            chip.append(texto, remover);
            chipsEl.appendChild(chip);
        });
    }

    function renderizarInputs() {
        inputsEl.innerHTML = "";
        selecionados.forEach(function (id) {
            var input = document.createElement("input");
            input.type = "hidden";
            input.name = "modelosCelularIds";
            input.value = id;
            inputsEl.appendChild(input);
        });
    }

    function renderizarResultados(termo) {
        var t = termo.trim().toLowerCase();
        var candidatos = itensBusca.filter(function (i) { return !selecionados.has(i.modeloId); });
        if (t) {
            candidatos = candidatos.filter(function (i) {
                return i.modeloNome.toLowerCase().includes(t) || i.marcaNome.toLowerCase().includes(t);
            });
        }
        candidatos = candidatos.slice(0, 8);

        resultadosEl.innerHTML = "";
        if (candidatos.length === 0) {
            resultadosEl.classList.add("hidden");
            return;
        }

        candidatos.forEach(function (i) {
            var opcao = document.createElement("button");
            opcao.type = "button";
            opcao.className = "w-full text-left px-md py-2 font-body-md text-body-md hover:bg-surface-container-low";
            opcao.textContent = i.marcaNome + " — " + i.modeloNome;
            opcao.addEventListener("click", function () {
                selecionados.add(i.modeloId);
                renderizarChips();
                renderizarInputs();
                buscaInput.value = "";
                resultadosEl.classList.add("hidden");
                buscaInput.focus();
            });
            resultadosEl.appendChild(opcao);
        });

        resultadosEl.classList.remove("hidden");
    }

    buscaInput.addEventListener("focus", function () { renderizarResultados(buscaInput.value); });
    buscaInput.addEventListener("input", function () { renderizarResultados(buscaInput.value); });

    chipsEl.addEventListener("click", function (e) {
        var btn = e.target.closest("[data-remover]");
        if (!btn) return;
        selecionados.delete(parseInt(btn.dataset.remover, 10));
        renderizarChips();
        renderizarInputs();
    });

    document.addEventListener("click", function (e) {
        if (!wrap.contains(e.target)) resultadosEl.classList.add("hidden");
    });

    renderizarChips();
    renderizarInputs();
});
