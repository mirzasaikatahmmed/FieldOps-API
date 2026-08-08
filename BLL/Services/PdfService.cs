using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FieldOps.BLL.Services;

public class PdfService : IPdfService
{
    private readonly IStorageService _storageService;

    public PdfService(IStorageService storageService)
    {
        _storageService = storageService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<(string StorageKey, string Url)> GenerateJobReportAsync(Job job, CancellationToken cancellationToken = default)
    {
        var photoBytes = new List<(string Caption, byte[] Bytes)>();
        foreach (var photo in job.Photos)
        {
            try
            {
                await using var stream = await _storageService.DownloadAsync(photo.StorageKey, cancellationToken);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, cancellationToken);
                photoBytes.Add((photo.Caption ?? "Photo", ms.ToArray()));
            }
            catch
            {
                // Skip photos that fail to download
            }
        }

        byte[]? signatureBytes = null;
        if (job.Signature is not null)
        {
            try
            {
                await using var stream = await _storageService.DownloadAsync(job.Signature.StorageKey, cancellationToken);
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms, cancellationToken);
                signatureBytes = ms.ToArray();
            }
            catch
            {
                // ignore
            }
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(col =>
                {
                    col.Item().Text(job.Company?.Name ?? "FieldOps").FontSize(20).SemiBold();
                    col.Item().Text("Field Service Inspection Report").FontSize(14).FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text(job.Title).FontSize(16).SemiBold();
                    col.Item().Text($"Customer: {job.Customer?.Name}");
                    if (!string.IsNullOrWhiteSpace(job.Customer?.Address))
                        col.Item().Text($"Address: {job.Customer.Address}");
                    col.Item().Text($"Technician: {job.AssignedTechnician?.FullName ?? "Unassigned"}");
                    col.Item().Text($"Scheduled: {job.ScheduledAt:u}");
                    col.Item().Text($"Completed: {(job.CompletedAt.HasValue ? job.CompletedAt.Value.ToString("u") : "N/A")}");
                    if (!string.IsNullOrWhiteSpace(job.Notes))
                        col.Item().Text($"Notes: {job.Notes}");

                    col.Item().PaddingTop(12).Text("Checklist Responses").FontSize(14).SemiBold();

                    var fields = job.JobTemplate.TemplateFields.OrderBy(f => f.SortOrder).ToList();
                    var responses = job.Responses.ToDictionary(r => r.TemplateFieldId);
                    foreach (var field in fields)
                    {
                        responses.TryGetValue(field.Id, out var response);
                        var answer = FormatAnswer(field, response);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"{field.Label}:").SemiBold();
                            row.RelativeItem(2).Text(answer);
                        });
                    }

                    if (photoBytes.Count > 0)
                    {
                        col.Item().PaddingTop(12).Text("Photos").FontSize(14).SemiBold();
                        foreach (var (caption, bytes) in photoBytes)
                        {
                            col.Item().Text(caption).Italic();
                            col.Item().MaxHeight(220).Image(bytes).FitArea();
                        }
                    }

                    if (job.Signature is not null)
                    {
                        col.Item().PaddingTop(12).Text("Signature").FontSize(14).SemiBold();
                        col.Item().Text($"Signed by: {job.Signature.SignedByName}");
                        col.Item().Text($"Signed at: {job.Signature.SignedAt:u}");
                        if (signatureBytes is not null)
                            col.Item().MaxHeight(120).Image(signatureBytes).FitArea();
                    }
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Generated by FieldOps • ");
                    txt.Span($"{DateTime.UtcNow:u}");
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        var storageKey = $"companies/{job.CompanyId}/jobs/{job.Id}/reports/{Guid.NewGuid()}.pdf";
        await using var uploadStream = new MemoryStream(pdfBytes);
        await _storageService.UploadAsync(storageKey, uploadStream, "application/pdf", cancellationToken);
        var url = _storageService.GetPublicUrl(storageKey);
        return (storageKey, url);
    }

    private static string FormatAnswer(TemplateField field, JobResponse? response)
    {
        if (response is null)
            return "—";

        return field.FieldType switch
        {
            FieldType.Text or FieldType.Select or FieldType.Signature => response.ValueText ?? "—",
            FieldType.Number => response.ValueNumber?.ToString() ?? "—",
            FieldType.Boolean => response.ValueBool switch
            {
                true => "Yes",
                false => "No",
                _ => "—"
            },
            FieldType.Photo => response.PhotoUrl ?? "—",
            _ => "—"
        };
    }
}
