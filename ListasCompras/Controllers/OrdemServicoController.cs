using System.Text.RegularExpressions;
using ListasCompras.Data;
using ListasCompras.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ListasCompras.Controllers;

// A ordem de serviço é o aparelho que ficou na loja: tem termos, assinatura das duas partes,
// garantia contada da retirada e as duas vias impressas.
public class OrdemServicoController : DocumentoControllerBase
{
    public OrdemServicoController(AppDbContext context) : base(context) { }

    protected override string Tipo => TiposDocumento.OrdemServico;

    // Abre uma OS nova já preenchida com o cliente e os aparelhos da original,
    // marcada como retorno. O reparo em garantia não se cobra de novo.
    public IActionResult RetornoGarantia(int id)
    {
        var original = Context.OrdensServico
            .Include(o => o.Cliente)
            .Include(o => o.Aparelhos)
            .FirstOrDefault(o => o.Id == id && o.Tipo == TiposDocumento.OrdemServico);

        if (original == null) return NotFound();

        var retorno = new OrdemServico
        {
            Tipo = TiposDocumento.OrdemServico,
            ClienteId = original.ClienteId,
            Cliente = original.Cliente,
            OrdemOrigemId = original.Id,
            OrdemOrigem = original,
            Diagnostico = $"Retorno em garantia da {original.Numero}. Defeito relatado: ",
        };

        foreach (var ap in original.Aparelhos)
        {
            retorno.Aparelhos.Add(new AparelhoOs
            {
                Tipo = ap.Tipo, Marca = ap.Marca, Modelo = ap.Modelo,
                NumeroSerie = ap.NumeroSerie, SemNumeroSerie = ap.SemNumeroSerie,
            });
        }

        ViewData["EhOrcamento"] = false;
        return View("~/Views/Documento/Add.cshtml", retorno);
    }

    // ===== Fotos do aparelho =====

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarFoto(int aparelhoId, IFormFile? foto)
    {
        var aparelho = Context.AparelhosOs
            .Include(a => a.OrdemServico)
            .FirstOrDefault(a => a.Id == aparelhoId && a.OrdemServico.Tipo == TiposDocumento.OrdemServico);

        if (aparelho == null) return NotFound();

        if (foto == null || !FotoAparelhoServico.TamanhoValido(foto.Length) || !FotoAparelhoServico.TipoValido(foto.ContentType))
        {
            TempData["Erro"] = "Envie uma foto em JPEG, PNG ou WEBP de até 8 MB.";
            return RedirectToAction(nameof(Ver), new { id = aparelho.OrdemServicoId });
        }

        await using var stream = foto.OpenReadStream();
        var arquivo = await FotoAparelhoServico.SalvarAsync(stream, foto.ContentType);

        Context.FotosAparelho.Add(new FotoAparelho { AparelhoOsId = aparelho.Id, Arquivo = arquivo });
        Context.SaveChanges();

        return RedirectToAction(nameof(Ver), new { id = aparelho.OrdemServicoId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExcluirFoto(int id)
    {
        var foto = Context.FotosAparelho
            .Include(f => f.AparelhoOs).ThenInclude(a => a.OrdemServico)
            .FirstOrDefault(f => f.Id == id && f.AparelhoOs.OrdemServico.Tipo == TiposDocumento.OrdemServico);

        if (foto == null) return NotFound();

        var ordemId = foto.AparelhoOs.OrdemServicoId;
        FotoAparelhoServico.Remover(foto.Arquivo);
        Context.FotosAparelho.Remove(foto);
        Context.SaveChanges();

        return RedirectToAction(nameof(Ver), new { id = ordemId });
    }

    // ===== Notificação ao cliente =====

    // Registra a notificação e só depois redireciona para o wa.me — a prova exigida pela
    // cláusula de abandono é a intenção de aviso registrada no sistema, não a confirmação
    // de entrega do WhatsApp, que o sistema não tem como saber.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult NotificarWhatsApp(int id, string mensagem)
    {
        var ordem = Context.OrdensServico
            .Include(o => o.Cliente)
            .FirstOrDefault(o => o.Id == id && o.Tipo == TiposDocumento.OrdemServico);

        if (ordem == null) return NotFound();

        var numero = NumeroWhatsApp(ordem.Cliente.Telefone);
        if (numero == null)
        {
            TempData["Erro"] = "Este cliente não tem telefone cadastrado.";
            return RedirectToAction(nameof(Ver), new { id });
        }

        var texto = string.IsNullOrWhiteSpace(mensagem) ? MensagemPadrao(ordem) : mensagem.Trim();

        Context.NotificacoesCliente.Add(new NotificacaoCliente
        {
            OrdemServicoId = ordem.Id,
            Canal = CanaisNotificacao.WhatsApp,
            Destinatario = numero,
            Mensagem = texto,
            UsuarioId = IdDoUsuarioLogado(),
        });
        Context.SaveChanges();

        var link = $"https://wa.me/{numero}?text={Uri.EscapeDataString(texto)}";
        return Redirect(link);
    }

    // DDI 55 fixo: só dígitos, e assume Brasil — mesma suposição que o resto do
    // cadastro de cliente já faz ao não pedir código de país
    private static string? NumeroWhatsApp(string? telefone)
    {
        var digitos = Regex.Replace(telefone ?? "", @"\D", "");
        if (digitos.Length < 10) return null;
        return digitos.StartsWith("55") ? digitos : $"55{digitos}";
    }

    private static string MensagemPadrao(OrdemServico ordem) =>
        ordem.Situacao == Situacoes.Pronta
            ? $"Olá, {ordem.Cliente.Nome}! Seu aparelho ({ordem.AparelhosResumo}) está pronto para retirada. Ordem {ordem.Numero}."
            : $"Olá, {ordem.Cliente.Nome}! Sobre a ordem {ordem.Numero} ({ordem.AparelhosResumo}) na nossa loja.";
}
