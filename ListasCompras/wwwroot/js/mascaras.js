// Máscaras de digitação compartilhadas por todo o sistema: telefone, CPF e valor em reais.
// Aplicadas por atributo data-mascara, então uma tela só precisa marcar o input — sem
// duplicar esta lógica em cada arquivo JS de formulário.
//
// Delegação no document (não um listener por elemento): telas como a de Orçamento criam
// linhas de item dinamicamente depois do carregamento, e ligar por elemento não alcançaria
// campos que ainda não existiam quando o script rodou.
(function () {
    function soDigitos(v) {
        return String(v || "").replace(/\D/g, "");
    }

    // (00) 00000-0000 — só até 11 dígitos; fixo (10) cai para (00) 0000-0000 sozinho
    function mascararTelefone(valor) {
        var d = soDigitos(valor).slice(0, 11);
        if (d.length === 0) return "";
        if (d.length <= 2) return "(" + d;
        if (d.length <= 6) return "(" + d.slice(0, 2) + ") " + d.slice(2);
        if (d.length <= 10) return "(" + d.slice(0, 2) + ") " + d.slice(2, 6) + "-" + d.slice(6);
        return "(" + d.slice(0, 2) + ") " + d.slice(2, 7) + "-" + d.slice(7);
    }

    // 000.000.000-00 — só até 11 dígitos
    function mascararCpf(valor) {
        var d = soDigitos(valor).slice(0, 11);
        if (d.length === 0) return "";
        if (d.length <= 3) return d;
        if (d.length <= 6) return d.slice(0, 3) + "." + d.slice(3);
        if (d.length <= 9) return d.slice(0, 3) + "." + d.slice(3, 6) + "." + d.slice(6);
        return d.slice(0, 3) + "." + d.slice(3, 6) + "." + d.slice(6, 9) + "-" + d.slice(9);
    }

    // 00.000.000/0000-00 — 14 dígitos
    function mascararCnpj(valor) {
        var d = soDigitos(valor).slice(0, 14);
        if (d.length === 0) return "";
        if (d.length <= 2) return d;
        if (d.length <= 5) return d.slice(0, 2) + "." + d.slice(2);
        if (d.length <= 8) return d.slice(0, 2) + "." + d.slice(2, 5) + "." + d.slice(5);
        if (d.length <= 12) return d.slice(0, 2) + "." + d.slice(2, 5) + "." + d.slice(5, 8) + "/" + d.slice(8);
        return d.slice(0, 2) + "." + d.slice(2, 5) + "." + d.slice(5, 8) + "/" + d.slice(8, 12) + "-" + d.slice(12);
    }

    // Documento da loja/do cliente pode ser CPF ou CNPJ: até 11 dígitos formata como CPF,
    // a partir do 12º já é CNPJ — é o próprio tamanho do número que diferencia os dois.
    function mascararCpfCnpj(valor) {
        return soDigitos(valor).length > 11 ? mascararCnpj(valor) : mascararCpf(valor);
    }

    // 00000-000
    function mascararCep(valor) {
        var d = soDigitos(valor).slice(0, 8);
        if (d.length <= 5) return d;
        return d.slice(0, 5) + "-" + d.slice(5);
    }

    // Dígitos entram da direita: "1" -> 0,01 · "12" -> 0,12 · "1250" -> 12,50
    function mascararValor(valor) {
        var d = soDigitos(valor).replace(/^0+(?=\d)/, "");
        d = d.padStart(3, "0");
        var inteiro = d.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, ".");
        return inteiro + "," + d.slice(-2);
    }

    function aplicar(input, novoValor) {
        // Máscara de valor sempre mexe da direita para a esquerda; o cursor fica no fim
        input.value = novoValor;
        input.setSelectionRange(novoValor.length, novoValor.length);
    }

    // capture: true é essencial aqui — faz este listener rodar ANTES dos listeners de
    // "input" que cada tela liga nos próprios campos (ex: recalcular() do orçamento).
    // Sem isso, o cálculo lê o valor daquele instante ainda não mascarado (o dígito recém
    // digitado sem a formatação aplicada) e erra a conta: foi o bug do orçamento somando
    // "150,00" como "1,50" — o total rodava um evento atrás do valor exibido.
    document.addEventListener("input", function (e) {
        var input = e.target;
        if (!(input instanceof HTMLInputElement)) return;
        var tipo = input.dataset.mascara;
        if (!tipo) return;

        if (tipo === "telefone") {
            aplicar(input, mascararTelefone(input.value));
        } else if (tipo === "cpf") {
            aplicar(input, mascararCpf(input.value));
        } else if (tipo === "cnpj") {
            aplicar(input, mascararCnpj(input.value));
        } else if (tipo === "cpf-cnpj") {
            aplicar(input, mascararCpfCnpj(input.value));
        } else if (tipo === "cpf-cnpj-opcional" || tipo === "cpf-opcional") {
            // Campo que também aceita RG: só formata como CPF/CNPJ enquanto o texto
            // digitado ainda é compatível (só dígitos, ponto, barra e traço) — RG com
            // letra não é mexido
            if (/^[\d.\/\-]*$/.test(input.value)) aplicar(input, mascararCpfCnpj(input.value));
        } else if (tipo === "cep") {
            aplicar(input, mascararCep(input.value));
        } else if (tipo === "valor") {
            aplicar(input, mascararValor(input.value));
        }
    }, true);

    // Campos que já chegam com valor (edição) mostram formatado desde o início; campo
    // vazio (nada digitado ainda) fica vazio até o usuário começar a digitar — não força
    // "0,00" onde vazio tem significado próprio (sem haver, sem desconto).
    document.addEventListener("DOMContentLoaded", function () {
        document.querySelectorAll('[data-mascara="valor"]').forEach(function (input) {
            if (input.value) aplicar(input, mascararValor(input.value));
        });
    });
})();
