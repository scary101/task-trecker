using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using steptreck.API.Models;
using steptreck.API.Infrastructure.File;
using steptreck.API.Infrastructure.Secutiry;
using steptreck.Domain.DTOs;

public class ReceiptPdfService
{
    private readonly AppDbContext _context;
    private readonly FileManagerHelper _files;
    private readonly UserHelper _userHelper;
    private readonly AuditService _auditService;

    public ReceiptPdfService(AppDbContext context, FileManagerHelper files, UserHelper userHelper, AuditService auditService)
    {
        _context = context;
        _files = files;
        _userHelper = userHelper;
        _auditService = auditService;
    }

    public async Task<string> GenerateAndStoreAsync(long paymentId, CancellationToken ct = default)
    {
        var payment = await _context.Payments
            .Include(p => p.Organization)
            .Include(p => p.Subscription)
                .ThenInclude(s => s!.Plan)
            .FirstOrDefaultAsync(p => p.Id == paymentId, ct);

        if (payment == null)
            throw new KeyNotFoundException("Платёж не найден.");

        if (!string.IsNullOrWhiteSpace(payment.ReceiptObjectKey))
            return payment.ReceiptObjectKey!;

        var pdfBytes = BuildPdf(payment);

        using var ms = new MemoryStream(pdfBytes);

        var objectKey =
            $"receipts/org-{payment.OrganizationId}/payment-{payment.Id}/receipt-{DateTime.UtcNow:yyyyMMddTHHmmss}.pdf";

        await _files.UploadAsync(objectKey, ms, "application/pdf", ct);

        payment.ReceiptObjectKey = objectKey;
        payment.ReceiptContentType = "application/pdf";
        payment.ReceiptCreatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        await _auditService.LogWithIdAsync(_userHelper.GetCurrentUserId(), payment.OrganizationId, new AuditLogCreateDto
        {
            Action = "update",
            EntityType = "payment_receipt",
            EntityId = payment.Id,
            Title = "Сформирован чек оплаты",
            NewValues = new
            {
                payment.ReceiptObjectKey,
                payment.ReceiptContentType,
                payment.ReceiptCreatedAt
            }
        }, ct);

        return objectKey;
    }
    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadReceiptAsync(
            long paymentId,
            CancellationToken ct = default)
    {
        var orgId = _userHelper.GetCurrentOrganizationId();
        var payment = await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.OrganizationId == orgId, ct);

        if (payment == null)
            throw new KeyNotFoundException("Платёж не найден.");

        if (string.IsNullOrWhiteSpace(payment.ReceiptObjectKey))
        {
            await GenerateAndStoreAsync(paymentId, ct);

            payment = await _context.Payments
                .AsNoTracking()
                .FirstAsync(p => p.Id == paymentId && p.OrganizationId == orgId, ct);
        }

        var objectKey = payment.ReceiptObjectKey!;
        var contentType = payment.ReceiptContentType ?? "application/pdf";
        var fileName = $"receipt-payment-{paymentId}.pdf";

        var stream = await _files.DownloadAsync(objectKey, ct); 

        return (stream, contentType, fileName);
    }


    private static byte[] BuildPdf(Payment p)
    {
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(12));

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text("Чек оплаты StepTreck")
                        .FontSize(20)
                        .Bold();

                    col.Item().Text($"Номер операции: #{p.Id}");
                    col.Item().Text($"Дата: {p.CreatedAt:dd.MM.yyyy HH:mm} UTC");
                    col.Item().Text($"Организация: {p.Organization?.Name ?? p.OrganizationId.ToString()}");

                    col.Item().LineHorizontal(1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(120);
                        });

                        table.Cell().Text("Тариф");
                        table.Cell().AlignRight().Text(p.Subscription?.Plan?.Name ?? "—");

                        table.Cell().Text("Период");
                        table.Cell().AlignRight().Text(
                            $"{p.Subscription?.StartDate:dd.MM.yyyy} — {p.Subscription?.EndDate:dd.MM.yyyy}");

                        table.Cell().Text("Провайдер");
                        table.Cell().AlignRight().Text(p.Provider);

                        table.Cell().Text("Статус");
                        table.Cell().AlignRight().Text(p.Status);

                        table.Cell().PaddingTop(10).Text("Итого").Bold();
                        table.Cell().PaddingTop(10).AlignRight()
                            .Text($"{p.AmountCents / 100m:0.00} {p.Currency}")
                            .Bold();
                    });

                    col.Item().PaddingTop(20)
                        .Text("Документ сформирован автоматически.")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        });

        return doc.GeneratePdf();


    }

}
