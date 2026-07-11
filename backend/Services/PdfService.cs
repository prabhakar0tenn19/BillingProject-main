using BillingSystem.DTOs;
using BillingSystem.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BillingSystem.Services;

/// <summary>
/// Generates professional GST-compliant Tax Invoice PDFs using QuestPDF.
///
/// PDF Layout (A4, portrait):
///   ┌───────────────────────────────────────────────────────┐
///   │  Company Name (large)    │  TAX INVOICE               │
///   │  Address, GSTIN, Phone   │  Invoice #, Date, Due Date │
///   ├───────────────────────────────────────────────────────┤
///   │  Bill To: Party Name, GSTIN, Address                  │
///   ├──────┬─────────┬───────┬─────┬────┬──────┬──────┬────┤
///   │  Sr  │ Item    │  HSN  │ Qty │Rate│Taxabl│CGST  │SGST│ Total │
///   ├──────┼─────────┼───────┼─────┼────┼──────┼──────┼────┤
///   │  ... │         │       │     │    │      │2.5%  │2.5%│       │
///   ├───────────────────────────────────────────────────────┤
///   │                    Taxable: XX  CGST: XX  SGST: XX    │
///   │                    GRAND TOTAL: ₹XX,XXX.XX            │
///   │  Amount in Words: ...                                 │
///   ├───────────────────────────────────────────────────────┤
///   │  Bank Details        │  [Signature Image]             │
///   │                      │  Authorized Signatory          │
///   └───────────────────────────────────────────────────────┘
///
/// Signature is fetched from Cloudinary URL and embedded as bytes in the PDF.
/// This ensures the PDF works offline and when printed.
/// </summary>
public class PdfService : IPdfService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PdfService> _logger;

    // Company brand colors
    private const string PrimaryColor   = "#1a2e5a";   // Deep navy blue
    private const string AccentColor    = "#2563eb";   // Bright blue
    private const string LightBg        = "#f8fafc";   // Very light grey
    private const string BorderColor    = "#e2e8f0";   // Soft border

    public PdfService(IHttpClientFactory httpClientFactory, ILogger<PdfService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger            = logger;
    }

    public async Task<byte[]> GenerateInvoicePdfAsync(InvoiceResponse invoice)
    {
        // Fetch digital signature from Cloudinary (if configured)
        byte[]? signatureBytes = await FetchSignatureAsync(invoice.CompanySnapshot.SignatureUrl);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // ── HEADER ────────────────────────────────────────────────
                    col.Item().PaddingBottom(8).Row(header =>
                    {
                        // Left: Company details
                        header.RelativeItem().Column(company =>
                        {
                            company.Item()
                                .Text(invoice.CompanySnapshot.CompanyName)
                                .Bold().FontSize(18).FontColor(PrimaryColor);

                            company.Item().PaddingTop(2)
                                .Text(invoice.CompanySnapshot.Address)
                                .FontSize(8).FontColor("#64748b");

                            company.Item().PaddingTop(2)
                                .Text($"GSTIN: {invoice.CompanySnapshot.Gstin}")
                                .FontSize(8).Bold();

                            company.Item()
                                .Text($"Phone: {invoice.CompanySnapshot.Phone}")
                                .FontSize(8);

                            if (!string.IsNullOrEmpty(invoice.CompanySnapshot.Email))
                                company.Item()
                                    .Text($"Email: {invoice.CompanySnapshot.Email}")
                                    .FontSize(8);
                        });

                        // Right: Invoice meta
                        header.ConstantItem(180).Column(meta =>
                        {
                            meta.Item().AlignRight()
                                .Background(PrimaryColor)
                                .Padding(8)
                                .Text("TAX INVOICE")
                                .Bold().FontSize(14).FontColor("#ffffff");

                            meta.Item().PaddingTop(6).AlignRight()
                                .Text($"Invoice No: {invoice.InvoiceNumber}")
                                .FontSize(9).Bold();

                            meta.Item().AlignRight()
                                .Text($"Date: {invoice.InvoiceDate:dd-MM-yyyy}")
                                .FontSize(9);

                            if (invoice.DueDate.HasValue)
                                meta.Item().AlignRight()
                                    .Text($"Due: {invoice.DueDate:dd-MM-yyyy}")
                                    .FontSize(9).FontColor("#dc2626");

                            meta.Item().PaddingTop(4).AlignRight()
                                .Background(invoice.Status == "paid" ? "#16a34a" : "#d97706")
                                .Padding(3)
                                .Text(invoice.Status.ToUpperInvariant())
                                .FontSize(8).Bold().FontColor("#ffffff");
                        });
                    });

                    // Divider
                    col.Item().BorderBottom(1.5f).BorderColor(PrimaryColor).PaddingBottom(8);

                    // ── BILL TO ───────────────────────────────────────────────
                    col.Item().PaddingVertical(8).Background(LightBg).Padding(8).Column(billTo =>
                    {
                        billTo.Item().Text("Bill To:").Bold().FontSize(8).FontColor("#64748b");
                        billTo.Item().Text(invoice.CustomerSnapshot.PartyName)
                            .Bold().FontSize(12).FontColor(PrimaryColor);

                        if (!string.IsNullOrEmpty(invoice.CustomerSnapshot.Gstin))
                            billTo.Item().Text($"GSTIN: {invoice.CustomerSnapshot.Gstin}").FontSize(8);

                        if (!string.IsNullOrEmpty(invoice.CustomerSnapshot.BillingAddress))
                            billTo.Item().Text(invoice.CustomerSnapshot.BillingAddress).FontSize(8);

                        if (!string.IsNullOrEmpty(invoice.CustomerSnapshot.Phone))
                            billTo.Item().Text($"Phone: {invoice.CustomerSnapshot.Phone}").FontSize(8);
                    });

                    col.Item().PaddingBottom(8);

                    // ── ITEMS TABLE ───────────────────────────────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(22);   // Sr.
                            cols.RelativeColumn(3);    // Description
                            cols.ConstantColumn(42);   // HSN
                            cols.ConstantColumn(35);   // Qty
                            cols.ConstantColumn(60);   // Rate
                            cols.ConstantColumn(65);   // Taxable Amt
                            cols.ConstantColumn(52);   // CGST 2.5%
                            cols.ConstantColumn(52);   // SGST 2.5%
                            cols.ConstantColumn(65);   // Total
                        });

                        // Table header row
                        static IContainer HeaderCellStyle(IContainer c) =>
                            c.Background("#1a2e5a").Padding(5);

                        void HeaderCell(string text, bool right = false)
                        {
                            var container = table.Cell().Element(HeaderCellStyle);
                            var aligned   = right ? container.AlignRight() : container.AlignCenter();
                            aligned.Text(text).FontSize(7.5f).Bold().FontColor("#ffffff");
                        }

                        HeaderCell("Sr.");
                        HeaderCell("Description & Model");
                        HeaderCell("HSN");
                        HeaderCell("Qty");
                        HeaderCell("Rate", true);
                        HeaderCell("Taxable", true);
                        HeaderCell("CGST 2.5%", true);
                        HeaderCell("SGST 2.5%", true);
                        HeaderCell("Total", true);

                        // Data rows
                        for (int idx = 0; idx < invoice.Items.Count; idx++)
                        {
                            var item  = invoice.Items[idx];
                            string bg = idx % 2 == 0 ? "#ffffff" : "#f8fafc";
                            decimal halfGst = Math.Round(item.GstAmount / 2, 2);

                            IContainer CellStyle(IContainer c) => c.Background(bg).Padding(4);

                            table.Cell().Element(CellStyle)
                                .Text((idx + 1).ToString()).FontSize(8).AlignCenter();

                            table.Cell().Element(CellStyle).Column(desc =>
                            {
                                desc.Item().Text(item.ProductName).FontSize(8).Bold();
                                desc.Item().Text(item.ModelNumber).FontSize(7).FontColor("#64748b");
                            });

                            table.Cell().Element(CellStyle)
                                .Text(item.HsnCode).FontSize(8).AlignCenter();

                            table.Cell().Element(CellStyle)
                                .Text(item.Quantity.ToString()).FontSize(8).AlignCenter();

                            table.Cell().Element(CellStyle)
                                .Text(item.Rate.ToString("F2")).FontSize(8).AlignRight();

                            table.Cell().Element(CellStyle)
                                .Text(item.SubTotal.ToString("F2")).FontSize(8).AlignRight();

                            table.Cell().Element(CellStyle)
                                .Text(halfGst.ToString("F2")).FontSize(8).AlignRight();

                            table.Cell().Element(CellStyle)
                                .Text(halfGst.ToString("F2")).FontSize(8).AlignRight();

                            table.Cell().Element(CellStyle)
                                .Text(item.LineTotal.ToString("F2")).FontSize(8).AlignRight().Bold();
                        }
                    });

                    // ── TOTALS SECTION ────────────────────────────────────────
                    col.Item().PaddingTop(4).AlignRight().Width(280).Column(totals =>
                    {
                        decimal cgst = Math.Round(invoice.TotalGst / 2, 2);
                        decimal sgst = invoice.TotalGst - cgst;

                        void TotalRow(string label, string value, bool bold = false, string? color = null)
                        {
                            totals.Item().BorderBottom(0.5f).BorderColor(BorderColor)
                                .PaddingVertical(3).Row(row =>
                                {
                                    var labelText = row.RelativeItem()
                                        .Text(label).FontSize(8.5f).AlignRight();
                                    if (bold) labelText.Bold();

                                    var valueText = row.ConstantItem(90)
                                        .PaddingLeft(8)
                                        .Text(value).FontSize(8.5f).AlignRight();
                                    if (bold) valueText.Bold();
                                    if (color != null) valueText.FontColor(color);
                                });
                        }

                        TotalRow("Taxable Amount:", $"₹ {invoice.SubTotal:N2}");
                        TotalRow("CGST @ 2.5%:", $"₹ {cgst:N2}");
                        TotalRow("SGST @ 2.5%:", $"₹ {sgst:N2}");

                        // Grand Total row with highlight
                        totals.Item().PaddingTop(2).Background(PrimaryColor).Padding(6).Row(row =>
                        {
                            row.RelativeItem()
                                .Text("GRAND TOTAL").FontSize(10).Bold().FontColor("#ffffff").AlignRight();
                            row.ConstantItem(90)
                                .PaddingLeft(8)
                                .Text($"₹ {invoice.GrandTotal:N2}").FontSize(10).Bold().FontColor("#ffffff").AlignRight();
                        });
                    });

                    // Amount in words
                    col.Item().PaddingTop(6)
                        .Background(LightBg).Padding(6)
                        .Text($"Amount in Words:  {invoice.TotalInWords}")
                        .Italic().FontSize(8.5f).FontColor(PrimaryColor);

                    // Remarks
                    if (!string.IsNullOrEmpty(invoice.Remarks))
                    {
                        col.Item().PaddingTop(6)
                            .Text($"Remarks: {invoice.Remarks}")
                            .FontSize(8).FontColor("#64748b");
                    }

                    // ── FOOTER: Bank + Signature ──────────────────────────────
                    col.Item().PaddingTop(20).Row(footer =>
                    {
                        // Bank details (left)
                        footer.RelativeItem().Column(bank =>
                        {
                            bank.Item().Text("Payment Details").Bold().FontSize(9).FontColor(PrimaryColor);
                            bank.Item().PaddingTop(4);

                            if (!string.IsNullOrEmpty(invoice.CompanySnapshot.BankName))
                            {
                                bank.Item().Text($"Bank: {invoice.CompanySnapshot.BankName}").FontSize(8);
                                bank.Item().Text($"A/C No: {invoice.CompanySnapshot.BankAccount}").FontSize(8);
                                bank.Item().Text($"IFSC: {invoice.CompanySnapshot.BankIfsc}").FontSize(8);
                            }
                        });

                        // Signature (right)
                        footer.ConstantItem(200).Column(sig =>
                        {
                            sig.Item().AlignRight()
                                .Text($"For {invoice.CompanySnapshot.CompanyName}")
                                .Bold().FontSize(8);

                            sig.Item().PaddingTop(2).AlignRight().Height(60).Column(sigBox =>
                            {
                                if (signatureBytes is { Length: > 0 })
                                {
                                    sigBox.Item().AlignRight().AlignBottom()
                                        .Height(55).Image(signatureBytes).FitHeight();
                                }
                                else
                                {
                                    // Empty placeholder if no signature uploaded
                                    sigBox.Item().Height(55).BorderBottom(0.5f).BorderColor("#94a3b8");
                                }
                            });

                            sig.Item().PaddingTop(4).AlignRight()
                                .BorderTop(1).BorderColor(PrimaryColor)
                                .PaddingTop(3)
                                .Text("Authorized Signatory")
                                .FontSize(8).FontColor(PrimaryColor);
                        });
                    });

                    // Legal footer
                    col.Item().PaddingTop(12).BorderTop(0.5f).BorderColor(BorderColor).PaddingTop(4)
                        .Text("This is a computer-generated invoice. Subject to jurisdiction of Rajkot courts.")
                        .FontSize(7).FontColor("#94a3b8").AlignCenter();
                });
            });
        }).GeneratePdf();
    }

    // ─── Private: Fetch Signature ─────────────────────────────────────────────

    private async Task<byte[]?> FetchSignatureAsync(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            return await client.GetByteArrayAsync(url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to fetch signature from Cloudinary: {Error}. PDF will be generated without signature.", ex.Message);
            return null;
        }
    }
}

// QuestPDF TextSpan extension for conditional Bold
internal static class QuestPdfExtensions
{
    public static TextSpanDescriptor When(this TextSpanDescriptor span, bool condition,
        Func<TextSpanDescriptor, TextSpanDescriptor> onTrue,
        Func<TextSpanDescriptor, TextSpanDescriptor> onFalse)
        => condition ? onTrue(span) : onFalse(span);
}
