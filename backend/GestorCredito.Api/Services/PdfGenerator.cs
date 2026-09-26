using System.Globalization;
using GestorCredito.Api.Models;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace GestorCredito.Api.Services;

public interface IPdfGenerator
{
    byte[] GenerarCronograma(Prospecto prospecto, Producto producto, Simulacion simulacion);
    byte[] GenerarAprobacionFinal(Prospecto prospecto, Producto producto, Simulacion simulacion);
}

/// <summary>
/// FR-016/FR-027: genera los dos PDF requeridos (cronograma y aprobación final) con
/// PdfSharpCore (research.md §2). Los montos se muestran en Soles (S/), fechas en
/// formato DD/MM/AAAA (Principio II de la constitución).
/// </summary>
public class PdfGenerator : IPdfGenerator
{
    private static readonly CultureInfo Ci = CultureInfo.InvariantCulture;

    private static readonly string[] Encabezados =
        ["N°", "Fecha Pago", "Saldo Inicial", "Amortización", "Interés", "Otros", "Pago Total", "Saldo Final"];

    private static readonly double[] AnchoColumnas = [30, 70, 80, 80, 60, 50, 70, 80];

    public byte[] GenerarCronograma(Prospecto prospecto, Producto producto, Simulacion simulacion)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var fontTitulo = new XFont("Arial", 16);
        var fontTexto = new XFont("Arial", 10);
        var fontTabla = new XFont("Arial", 8);

        double y = 40;
        gfx.DrawString("Cronograma de Pagos", fontTitulo, XBrushes.Black, new XPoint(40, y));
        y += 25;
        gfx.DrawString($"Prospecto: {prospecto.Nombres} {prospecto.Apellidos}", fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 15;
        gfx.DrawString($"Documento: {prospecto.NumeroDocumento}", fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 15;
        gfx.DrawString($"Producto: {producto.Nombre}", fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 15;
        gfx.DrawString(
            $"Monto: S/ {simulacion.Monto.ToString("N2", Ci)}   Tasa: {simulacion.Tasa.ToString("N2", Ci)}%   Plazo: {simulacion.Plazo} cuotas",
            fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 25;

        y = DibujarEncabezadoTabla(gfx, fontTabla, y);

        decimal totalAPagar = 0;
        foreach (var cuota in simulacion.Cuotas.OrderBy(c => c.Numero))
        {
            var valores = new[]
            {
                cuota.Numero.ToString(Ci),
                cuota.FechaPago.ToString("dd/MM/yyyy", Ci),
                cuota.SaldoInicial.ToString("N2", Ci),
                cuota.Amortizacion.ToString("N2", Ci),
                cuota.Interes.ToString("N2", Ci),
                cuota.Otros.ToString("N2", Ci),
                cuota.PagoTotal.ToString("N2", Ci),
                cuota.SaldoFinal.ToString("N2", Ci),
            };
            DibujarFilaTabla(gfx, fontTabla, valores, y);
            totalAPagar += cuota.PagoTotal;
            y += 16;
        }

        y += 10;
        gfx.DrawString($"Total a pagar: S/ {totalAPagar.ToString("N2", Ci)}", fontTexto, XBrushes.Black, new XPoint(40, y));

        return GuardarComoBytes(document);
    }

    public byte[] GenerarAprobacionFinal(Prospecto prospecto, Producto producto, Simulacion simulacion)
    {
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var fontTitulo = new XFont("Arial", 16);
        var fontTexto = new XFont("Arial", 11);

        double y = 40;
        gfx.DrawString("Documento de Aprobación Final de Desembolso", fontTitulo, XBrushes.Black, new XPoint(40, y));
        y += 30;
        gfx.DrawString($"Cliente: {prospecto.Nombres} {prospecto.Apellidos}", fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 18;
        gfx.DrawString($"Documento: {prospecto.NumeroDocumento}", fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 18;
        gfx.DrawString($"Dirección: {prospecto.Direccion}", fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 18;
        gfx.DrawString(DescribirCuentaDesembolso(prospecto), fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 25;
        gfx.DrawString($"Producto: {producto.Nombre}", fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 18;
        gfx.DrawString(
            $"Monto aprobado: S/ {simulacion.Monto.ToString("N2", Ci)}   Tasa: {simulacion.Tasa.ToString("N2", Ci)}%   Plazo: {simulacion.Plazo} cuotas",
            fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 18;
        var totalAPagar = simulacion.Cuotas.Sum(c => c.PagoTotal);
        gfx.DrawString($"Total a pagar: S/ {totalAPagar.ToString("N2", Ci)}", fontTexto, XBrushes.Black, new XPoint(40, y));
        y += 30;
        gfx.DrawString(
            $"Estado: {prospecto.Estado} — Fecha de generación: {DateTime.Now.ToString("dd/MM/yyyy", Ci)}",
            fontTexto, XBrushes.Black, new XPoint(40, y));

        return GuardarComoBytes(document);
    }

    /// <summary>FR-011 (spec 002): describe la Cuenta Bancaria de Desembolso vigente (Interna
    /// o Externa) en vez del campo único CCI que se usaba en 001-gestion-creditos.</summary>
    private static string DescribirCuentaDesembolso(Prospecto prospecto) => prospecto.TipoCuentaDesembolso switch
    {
        TipoCuentaDesembolso.Interna when prospecto.CuentaBancariaInterna is not null =>
            $"Cuenta de Desembolso (Interna): N° {prospecto.CuentaBancariaInterna.NumeroCuenta} — {prospecto.CuentaBancariaInterna.Moneda}",
        TipoCuentaDesembolso.Externa =>
            $"Cuenta de Desembolso (Externa): {prospecto.CuentaExternaBanco} — CCI {prospecto.CuentaExternaCCI}",
        _ => "Cuenta de Desembolso: sin datos"
    };

    private static double DibujarEncabezadoTabla(XGraphics gfx, XFont font, double y)
    {
        double x = 40;
        for (var i = 0; i < Encabezados.Length; i++)
        {
            gfx.DrawString(Encabezados[i], font, XBrushes.Black, new XPoint(x, y));
            x += AnchoColumnas[i];
        }
        return y + 14;
    }

    private static void DibujarFilaTabla(XGraphics gfx, XFont font, string[] valores, double y)
    {
        double x = 40;
        for (var i = 0; i < valores.Length; i++)
        {
            gfx.DrawString(valores[i], font, XBrushes.Black, new XPoint(x, y));
            x += AnchoColumnas[i];
        }
    }

    private static byte[] GuardarComoBytes(PdfDocument document)
    {
        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }
}
