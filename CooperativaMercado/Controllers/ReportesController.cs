using ClosedXML.Excel;
using ClosedXML.Excel;
using CooperativaMercado.Repository.Dao;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CooperativaMercado.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportesController : ControllerBase
    {
        private readonly DeudaDao _deudaDao;
        private readonly PagoDao _pagoDao;
        private readonly SocioDao _socioDao;

        public ReportesController(DeudaDao deudaDao, PagoDao pagoDao, SocioDao socioDao)
        {
            _deudaDao = deudaDao;
            _pagoDao = pagoDao;
            _socioDao = socioDao;
        }


        [HttpGet("excel-deudas")]
        public IActionResult DeudasExcel()
        {
            var deudas = _deudaDao.Listar(); 

            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Deudas Detalladas");

            // HEADER con formato
            var header = new string[] { 
                "ID Deuda", "Puesto", "Tipo Deuda", "Descripción", 
                "Monto", "Mora", "Total", "Mes", "Año", 
                "Fecha Vencimiento", "Estado", "Socio (si aplica)" 
            };

            for (int i = 0; i < header.Length; i++)
            {
                var cell = hoja.Cell(1, i + 1);
                cell.Value = header[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            // DATA
            int fila = 2;
            foreach (var d in deudas)
            {
                hoja.Cell(fila, 1).Value = d.IdDeuda;
                hoja.Cell(fila, 2).Value = d.NumeroPuesto ?? "N/A";
                hoja.Cell(fila, 3).Value = d.NombreTipoDeuda ?? "N/A";
                hoja.Cell(fila, 4).Value = d.Descripcion ?? "";
                hoja.Cell(fila, 5).Value = d.Monto;
                hoja.Cell(fila, 5).Style.NumberFormat.Format = "#,##0.00";
                hoja.Cell(fila, 6).Value = d.Mora;
                hoja.Cell(fila, 6).Style.NumberFormat.Format = "#,##0.00";
                hoja.Cell(fila, 7).Value = d.MontoTotal;
                hoja.Cell(fila, 7).Style.NumberFormat.Format = "#,##0.00";
                hoja.Cell(fila, 8).Value = d.Mes;
                hoja.Cell(fila, 9).Value = d.Anio;
                hoja.Cell(fila, 10).Value = d.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "Sin fecha";
                hoja.Cell(fila, 11).Value = d.Estado;


                if (d.Estado == "Pagado")
                    hoja.Cell(fila, 11).Style.Font.FontColor = XLColor.Green;
                else if (d.Estado == "Pendiente")
                    hoja.Cell(fila, 11).Style.Font.FontColor = XLColor.Red;


                hoja.Cell(fila, 12).Value = ""; 

                fila++;
            }


            hoja.Columns().AdjustToContents();


            int filaResumen = fila + 2;
            hoja.Cell(filaResumen, 1).Value = "RESUMEN:";
            hoja.Cell(filaResumen, 1).Style.Font.Bold = true;

            hoja.Cell(filaResumen + 1, 1).Value = "Total Deudas:";
            hoja.Cell(filaResumen + 1, 2).Value = deudas.Count;

            hoja.Cell(filaResumen + 2, 1).Value = "Deudas Pagadas:";
            hoja.Cell(filaResumen + 2, 2).Value = deudas.Count(d => d.Estado == "Pagado");
            hoja.Cell(filaResumen + 2, 2).Style.Font.FontColor = XLColor.Green;

            hoja.Cell(filaResumen + 3, 1).Value = "Deudas Pendientes:";
            hoja.Cell(filaResumen + 3, 2).Value = deudas.Count(d => d.Estado == "Pendiente");
            hoja.Cell(filaResumen + 3, 2).Style.Font.FontColor = XLColor.Red;

            hoja.Cell(filaResumen + 4, 1).Value = "Monto Total:";
            hoja.Cell(filaResumen + 4, 2).Value = deudas.Sum(d => d.MontoTotal);
            hoja.Cell(filaResumen + 4, 2).Style.NumberFormat.Format = "S/ #,##0.00";
            hoja.Cell(filaResumen + 4, 2).Style.Font.Bold = true;

            using var ms = new MemoryStream();
            libro.SaveAs(ms);
            var fechaActual = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return File(ms.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"Deudas_Detalladas_{fechaActual}.xlsx");
        }

   
        [HttpGet("pdf-pagos")]
        public IActionResult PagosPdf()
        {
            var pagos = _pagoDao.Listar();
            var totalRecaudado = pagos.Sum(p => p.Monto);
            var fechaActual = DateTime.Now;

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);

                    // HEADER
                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text("HISTORIAL COMPLETO DE PAGOS").FontSize(20).Bold();
                        header.Item().AlignCenter().Text("Cooperativa de Mercado").FontSize(14);
                        header.Item().PaddingTop(5).AlignCenter().Text($"Generado: {fechaActual:dd/MM/yyyy HH:mm}").FontSize(10);
                        header.Item().PaddingVertical(10).LineHorizontal(2);
                    });

                    // CONTENT
                    page.Content().Column(col =>
                    {
                        // Resumen superior
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Total de Pagos: {pagos.Count}").Bold();
                                c.Item().Text($"Total Recaudado: S/ {totalRecaudado:N2}").FontSize(14).Bold().FontColor(Colors.Green.Medium);
                            });
                        });

                        col.Item().PaddingVertical(10);

                        // Tabla de pagos
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(40);  // ID
                                columns.RelativeColumn();     // Recibo
                                columns.RelativeColumn();     // Puesto
                                columns.RelativeColumn(2);    // Concepto
                                columns.RelativeColumn();     // Fecha
                                columns.RelativeColumn();     // Método
                                columns.RelativeColumn();     // Monto
                            });

                            // Header
                            table.Header(h =>
                            {
                                h.Cell().Element(CellStyle).Text("ID").Bold();
                                h.Cell().Element(CellStyle).Text("N° Recibo").Bold();
                                h.Cell().Element(CellStyle).Text("Puesto").Bold();
                                h.Cell().Element(CellStyle).Text("Concepto").Bold();
                                h.Cell().Element(CellStyle).Text("Fecha").Bold();
                                h.Cell().Element(CellStyle).Text("Método").Bold();
                                h.Cell().Element(CellStyle).AlignRight().Text("Monto").Bold();
                            });

                            // Rows
                            foreach (var p in pagos)
                            {
                                table.Cell().Element(CellStyle).Text($"{p.IdPago}");
                                table.Cell().Element(CellStyle).Text(p.NumeroRecibo ?? "N/A");
                                table.Cell().Element(CellStyle).Text(p.NumeroPuesto ?? "N/A");
                                table.Cell().Element(CellStyle).Text(p.ConceptoDeuda ?? "N/A");
                                table.Cell().Element(CellStyle).Text(p.Fecha.ToString("dd/MM/yyyy"));
                                table.Cell().Element(CellStyle).Text(p.MetodoPago ?? "N/A");
                                table.Cell().Element(CellStyle).AlignRight().Text($"S/ {p.Monto:N2}");
                            }

                            // Footer de tabla con total
                            table.Cell().ColumnSpan(6).Element(CellStyle).AlignRight().Text("TOTAL RECAUDADO:").Bold().FontSize(12);
                            table.Cell().Element(CellStyle).Background(Colors.Grey.Lighten3).AlignRight().Text($"S/ {totalRecaudado:N2}").Bold().FontSize(12);
                        });
                    });

                    // FOOTER
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();

            var fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return File(documento, "application/pdf", $"Pagos_Historial_{fecha}.pdf");
        }


        [HttpGet("boleta/{idDeuda}")] 
        public IActionResult GenerarBoleta(int idDeuda)
        {
            var p = _pagoDao.ObtenerDatosBoleta(idDeuda);
            if (p == null)
                return NotFound("Boleta no disponible: la deuda no existe, aún está pendiente o no tiene un pago registrado.");

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A6);
                    page.Margin(0.5f, Unit.Centimetre);

                    page.Header().Text("BOLETA DE PAGO").FontSize(14).Bold().AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Item().Text($"Recibo: {p.NumeroRecibo}").FontSize(10);
                        col.Item().Text($"Fecha: {p.Fecha.ToShortDateString()}").FontSize(10);
                        col.Item().LineHorizontal(1);
                        col.Item().Text($"Puesto: {p.NumeroPuesto}");
                        col.Item().Text($"Socio: {p.NombreSocio}");
                        col.Item().Text($"Concepto: {p.ConceptoDeuda}");
                        col.Item().Text($"Método: {p.MetodoPago}");
                        col.Item().LineHorizontal(1);
                        col.Item().AlignRight().Text($"TOTAL: {p.Monto:C}").FontSize(12).Bold();
                    });

                    page.Footer().AlignCenter().Text("Gracias por su pago").FontSize(8);
                });
            }).GeneratePdf();

            return File(documento, "application/pdf", $"Boleta_{p.NumeroRecibo}.pdf");
        }

        private static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).PaddingHorizontal(3);
        }

        [HttpGet("excel-deudas-pendientes")]
        public IActionResult DeudasPendientesExcel()
        {
            var deudas = _deudaDao.ObtenerPendientes();

            using var libro = new XLWorkbook();
            var hoja = libro.Worksheets.Add("Deudas Pendientes");

            // HEADER
            var header = new string[] { 
                "ID", "Puesto", "Socio", "DNI", "Tipo Deuda", "Descripción", 
                "Monto", "Mora", "Total", "Mes/Año", "Vencimiento", "Días Vencido" 
            };

            for (int i = 0; i < header.Length; i++)
            {
                var cell = hoja.Cell(1, i + 1);
                cell.Value = header[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FF6B6B");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            int fila = 2;
            decimal totalDeuda = 0;
            decimal totalMora = 0;

            foreach (var d in deudas)
            {
                hoja.Cell(fila, 1).Value = d.IdDeuda;
                hoja.Cell(fila, 2).Value = d.NumeroPuesto ?? "N/A";

                hoja.Cell(fila, 3).Value = ""; // Socio
                hoja.Cell(fila, 4).Value = ""; // DNI

                hoja.Cell(fila, 5).Value = d.NombreTipoDeuda ?? "N/A";
                hoja.Cell(fila, 6).Value = d.Descripcion ?? "";
                hoja.Cell(fila, 7).Value = d.Monto;
                hoja.Cell(fila, 7).Style.NumberFormat.Format = "S/ #,##0.00";
                hoja.Cell(fila, 8).Value = d.Mora;
                hoja.Cell(fila, 8).Style.NumberFormat.Format = "S/ #,##0.00";
                hoja.Cell(fila, 9).Value = d.MontoTotal;
                hoja.Cell(fila, 9).Style.NumberFormat.Format = "S/ #,##0.00";
                hoja.Cell(fila, 9).Style.Font.Bold = true;
                hoja.Cell(fila, 10).Value = $"{d.Mes:00}/{d.Anio}";
                hoja.Cell(fila, 11).Value = d.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "Sin vencimiento";

                // Calcular días vencidos
                if (d.FechaVencimiento.HasValue && d.FechaVencimiento.Value < DateTime.Now)
                {
                    var diasVencido = (DateTime.Now - d.FechaVencimiento.Value).Days;
                    hoja.Cell(fila, 12).Value = diasVencido;
                    if (diasVencido > 30)
                        hoja.Cell(fila, 12).Style.Font.FontColor = XLColor.Red;
                    else if (diasVencido > 0)
                        hoja.Cell(fila, 12).Style.Font.FontColor = XLColor.Orange;
                }
                else
                {
                    hoja.Cell(fila, 12).Value = "0";
                }

                totalDeuda += d.Monto;
                totalMora += d.Mora;
                fila++;
            }

            hoja.Columns().AdjustToContents();

            int filaResumen = fila + 2;
            hoja.Cell(filaResumen, 1).Value = "RESUMEN";
            hoja.Cell(filaResumen, 1).Style.Font.Bold = true;
            hoja.Cell(filaResumen, 1).Style.Font.FontSize = 14;

            hoja.Cell(filaResumen + 1, 1).Value = "Total Deudas Pendientes:";
            hoja.Cell(filaResumen + 1, 2).Value = deudas.Count;
            hoja.Cell(filaResumen + 1, 2).Style.Font.Bold = true;

            hoja.Cell(filaResumen + 2, 1).Value = "Monto Deuda:";
            hoja.Cell(filaResumen + 2, 2).Value = totalDeuda;
            hoja.Cell(filaResumen + 2, 2).Style.NumberFormat.Format = "S/ #,##0.00";

            hoja.Cell(filaResumen + 3, 1).Value = "Monto Mora:";
            hoja.Cell(filaResumen + 3, 2).Value = totalMora;
            hoja.Cell(filaResumen + 3, 2).Style.NumberFormat.Format = "S/ #,##0.00";
            hoja.Cell(filaResumen + 3, 2).Style.Font.FontColor = XLColor.Red;

            hoja.Cell(filaResumen + 4, 1).Value = "TOTAL A COBRAR:";
            hoja.Cell(filaResumen + 4, 2).Value = totalDeuda + totalMora;
            hoja.Cell(filaResumen + 4, 2).Style.NumberFormat.Format = "S/ #,##0.00";
            hoja.Cell(filaResumen + 4, 2).Style.Font.Bold = true;
            hoja.Cell(filaResumen + 4, 2).Style.Font.FontSize = 14;
            hoja.Cell(filaResumen + 4, 2).Style.Fill.BackgroundColor = XLColor.Yellow;

            using var ms = new MemoryStream();
            libro.SaveAs(ms);
            var fechaActual = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return File(ms.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                $"Deudas_Pendientes_{fechaActual}.xlsx");
        }

        [HttpGet("pdf-deudas-pendientes")]
        public IActionResult DeudasPendientesPdf()
        {
            var deudas = _deudaDao.ObtenerPendientes();
            var totalDeuda = deudas.Sum(d => d.Monto);
            var totalMora = deudas.Sum(d => d.Mora);
            var totalGeneral = deudas.Sum(d => d.MontoTotal);
            var fechaActual = DateTime.Now;

            var documento = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(1, Unit.Centimetre);

                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text("REPORTE DE DEUDAS PENDIENTES").FontSize(20).Bold();
                        header.Item().AlignCenter().Text("Cooperativa de Mercado").FontSize(14);
                        header.Item().PaddingTop(5).AlignCenter().Text($"Generado: {fechaActual:dd/MM/yyyy HH:mm}").FontSize(10);
                        header.Item().PaddingVertical(10).LineHorizontal(2);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Background(Colors.Red.Lighten4).Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text($"Total Deudas: {deudas.Count}").FontSize(12).Bold();
                                c.Item().Text($"Monto Deuda: S/ {totalDeuda:N2}").FontSize(11);
                                c.Item().Text($"Monto Mora: S/ {totalMora:N2}").FontColor(Colors.Red.Medium);
                            });
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().AlignRight().Text("TOTAL A COBRAR").FontSize(12).Bold();
                                c.Item().AlignRight().Text($"S/ {totalGeneral:N2}").FontSize(18).Bold().FontColor(Colors.Red.Darken2);
                            });
                        });

                        col.Item().PaddingVertical(10);


                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(30);   // ID
                                columns.RelativeColumn();      // Puesto
                                columns.RelativeColumn(2);     // Tipo
                                columns.RelativeColumn();      // Monto
                                columns.RelativeColumn();      // Mora
                                columns.RelativeColumn();      // Total
                                columns.RelativeColumn();      // Mes/Año
                                columns.RelativeColumn();      // Vencimiento
                            });

                            // Header
                            table.Header(h =>
                            {
                                h.Cell().Element(HeaderStyle).Text("ID").Bold();
                                h.Cell().Element(HeaderStyle).Text("Puesto").Bold();
                                h.Cell().Element(HeaderStyle).Text("Tipo Deuda").Bold();
                                h.Cell().Element(HeaderStyle).AlignRight().Text("Monto").Bold();
                                h.Cell().Element(HeaderStyle).AlignRight().Text("Mora").Bold();
                                h.Cell().Element(HeaderStyle).AlignRight().Text("Total").Bold();
                                h.Cell().Element(HeaderStyle).AlignCenter().Text("Periodo").Bold();
                                h.Cell().Element(HeaderStyle).AlignCenter().Text("Vencimiento").Bold();
                            });

                            // Rows
                            foreach (var d in deudas)
                            {
                                table.Cell().Element(CellStyle).Text($"{d.IdDeuda}");
                                table.Cell().Element(CellStyle).Text(d.NumeroPuesto ?? "N/A");
                                table.Cell().Element(CellStyle).Text(d.NombreTipoDeuda ?? "N/A");
                                table.Cell().Element(CellStyle).AlignRight().Text($"S/ {d.Monto:N2}");

                                var moraCell = table.Cell().Element(CellStyle).AlignRight();
                                if (d.Mora > 0)
                                    moraCell.Text($"S/ {d.Mora:N2}").FontColor(Colors.Red.Medium);
                                else
                                    moraCell.Text("S/ 0.00");

                                table.Cell().Element(CellStyle).AlignRight().Text($"S/ {d.MontoTotal:N2}").Bold();
                                table.Cell().Element(CellStyle).AlignCenter().Text($"{d.Mes:00}/{d.Anio}");

                                var vencCell = table.Cell().Element(CellStyle).AlignCenter();
                                if (d.FechaVencimiento.HasValue)
                                {
                                    var fechaVenc = d.FechaVencimiento.Value.ToString("dd/MM/yy");
                                    if (d.FechaVencimiento.Value < DateTime.Now)
                                        vencCell.Text(fechaVenc).FontColor(Colors.Red.Medium);
                                    else
                                        vencCell.Text(fechaVenc);
                                }
                                else
                                {
                                    vencCell.Text("Sin fecha");
                                }
                            }
                        });
                    });

           
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                        text.Span($" - {fechaActual:dd/MM/yyyy}");
                    });
                });
            }).GeneratePdf();

            var fecha = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return File(documento, "application/pdf", $"Deudas_Pendientes_{fecha}.pdf");
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container
                .Border(1)
                .BorderColor(Colors.Grey.Medium)
                .Background(Colors.Grey.Lighten3)
                .PaddingVertical(5)
                .PaddingHorizontal(3);
        }
    }
}
