using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UniPilot.Application.Documents;
using UniPilot.Domain.Entities;
using UniPilot.Infrastructure.Persistence;
using UniPilot.Application.Notifications;

namespace UniPilot.Infrastructure.Documents;

public sealed class ProjectDocumentService(
    AppDbContext dbContext,
    IFileStorage fileStorage,
    IPdfTextExtractor pdfTextExtractor,
    INotificationService notificationService,
    ILogger<ProjectDocumentService> logger)
    : IProjectDocumentService
{
    public async Task<UploadDocumentResult>
        UploadAsync(
            UploadProjectDocumentCommand command,
            CancellationToken cancellationToken = default)
    {
        var ownsProject =
            await dbContext
                .AcademicProjects
                .AnyAsync(
                    project =>
                        project.Id ==
                            command
                                .AcademicProjectId &&
                        project.Course.OwnerId ==
                            command.OwnerId,
                    cancellationToken);

        if (!ownsProject)
        {
            return new UploadDocumentResult(
                UploadDocumentStatus
                    .ProjectNotFound,
                null);
        }

        if (!command.Content.CanSeek)
        {
            throw new InvalidOperationException(
                "The uploaded content must support seeking.");
        }

        command.Content.Position = 0;

        using var sha256 =
            SHA256.Create();

        var hashBytes =
            await sha256.ComputeHashAsync(
                command.Content,
                cancellationToken);

        var contentHash =
            Convert
                .ToHexString(hashBytes)
                .ToLowerInvariant();

        command.Content.Position = 0;

        var duplicateExists =
            await dbContext
                .ProjectDocuments
                .AnyAsync(
                    document =>
                        document
                            .AcademicProjectId ==
                            command
                                .AcademicProjectId &&
                        document.ContentHash ==
                            contentHash,
                    cancellationToken);

        if (duplicateExists)
        {
            return new UploadDocumentResult(
                UploadDocumentStatus.Duplicate,
                null);
        }

        var document =
            new ProjectDocument
            {
                AcademicProjectId =
                    command.AcademicProjectId,
                OriginalFileName =
                    command.OriginalFileName,
                ContentType =
                    command.ContentType,
                FileSizeBytes =
                    command.FileSizeBytes,
                ContentHash = contentHash,
                DocumentType =
                    command.DocumentType,
                ProcessingStatus =
                    DocumentProcessingStatus
                        .Uploaded
            };

        document.StorageKey =
            $"{command.AcademicProjectId:N}/{document.Id:N}.pdf";

        await fileStorage.SaveAsync(
            document.StorageKey,
            command.Content,
            cancellationToken);

        try
        {
            dbContext.ProjectDocuments.Add(
                document);

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch
        {
            await fileStorage.DeleteAsync(
                document.StorageKey,
                CancellationToken.None);

            throw;
        }

        await ExtractAndSavePagesAsync(
    document,
    cancellationToken);

await CreateProcessingNotificationAsync(
    command.OwnerId,
    document,
    false,
    cancellationToken);

return new UploadDocumentResult(
            UploadDocumentStatus.Success,
            Map(document));
    }

    public async Task<
        IReadOnlyList<ProjectDocumentResult>?>
        GetByProjectAsync(
            Guid ownerId,
            Guid academicProjectId,
            CancellationToken cancellationToken = default)
    {
        var ownsProject =
            await dbContext
                .AcademicProjects
                .AnyAsync(
                    project =>
                        project.Id ==
                            academicProjectId &&
                        project.Course.OwnerId ==
                            ownerId,
                    cancellationToken);

        if (!ownsProject)
        {
            return null;
        }

        return await dbContext
            .ProjectDocuments
            .AsNoTracking()
            .Where(document =>
                document.AcademicProjectId ==
                    academicProjectId)
            .OrderByDescending(document =>
                document.UploadedAtUtc)
            .Select(document =>
                new ProjectDocumentResult(
                    document.Id,
                    document
                        .AcademicProjectId,
                    document.OriginalFileName,
                    document.DocumentType
                        .ToString(),
                    document.ProcessingStatus
                        .ToString(),
                    document.FileSizeBytes,
                    document.PageCount,
                    document.FailureReason,
                    document.UploadedAtUtc))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<
        IReadOnlyList<DocumentPageResult>?>
        GetPagesAsync(
            Guid ownerId,
            Guid documentId,
            CancellationToken cancellationToken = default)
    {
        var ownsDocument =
            await dbContext
                .ProjectDocuments
                .AnyAsync(
                    document =>
                        document.Id ==
                            documentId &&
                        document
                            .AcademicProject
                            .Course
                            .OwnerId ==
                            ownerId,
                    cancellationToken);

        if (!ownsDocument)
        {
            return null;
        }

        return await dbContext
            .DocumentPages
            .AsNoTracking()
            .Where(page =>
                page.ProjectDocumentId ==
                    documentId)
            .OrderBy(page =>
                page.PageNumber)
            .Select(page =>
                new DocumentPageResult(
                    page.PageNumber,
                    page.Text))
            .ToListAsync(
                cancellationToken);
    }
public async Task<ProjectDocumentFileResult?>
    GetFileAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid documentId,
        CancellationToken cancellationToken = default)
{
    var document =
        await dbContext.ProjectDocuments
            .AsNoTracking()
            .Where(existingDocument =>
                existingDocument.Id ==
                    documentId &&
                existingDocument
                    .AcademicProjectId ==
                    academicProjectId &&
                existingDocument
                    .AcademicProject
                    .Course
                    .OwnerId ==
                    ownerId)
            .Select(existingDocument =>
                new
                {
                    existingDocument
                        .StorageKey,
                    existingDocument
                        .ContentType,
                    existingDocument
                        .OriginalFileName
                })
            .SingleOrDefaultAsync(
                cancellationToken);

    if (document is null)
    {
        return null;
    }

    var content =
        await fileStorage.OpenReadAsync(
            document.StorageKey,
            cancellationToken);

    return new ProjectDocumentFileResult(
        content,
        string.IsNullOrWhiteSpace(
            document.ContentType)
            ? "application/pdf"
            : document.ContentType,
        document.OriginalFileName);
}
public async Task<RetryDocumentProcessingResult>
    RetryProcessingAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid documentId,
        CancellationToken cancellationToken = default)
{
    var document =
        await dbContext.ProjectDocuments
            .SingleOrDefaultAsync(
                existingDocument =>
                    existingDocument.Id ==
                        documentId &&
                    existingDocument
                        .AcademicProjectId ==
                        academicProjectId &&
                    existingDocument
                        .AcademicProject
                        .Course
                        .OwnerId ==
                        ownerId,
                cancellationToken);

    if (document is null)
    {
        return new RetryDocumentProcessingResult(
            RetryDocumentProcessingStatus
                .DocumentNotFound,
            null);
    }

    if (
        document.ProcessingStatus !=
        DocumentProcessingStatus.Failed
    )
    {
        return new RetryDocumentProcessingResult(
            RetryDocumentProcessingStatus
                .DocumentNotFailed,
            Map(document));
    }

    var existingPages =
        await dbContext.DocumentPages
            .Where(page =>
                page.ProjectDocumentId ==
                    document.Id)
            .ToListAsync(
                cancellationToken);

    dbContext.DocumentPages.RemoveRange(
        existingPages);

    document.PageCount = 0;
    document.FailureReason = null;
    document.ProcessingStatus =
        DocumentProcessingStatus.Uploaded;

    await dbContext.SaveChangesAsync(
        cancellationToken);

    await ExtractAndSavePagesAsync(
    document,
    cancellationToken);

await CreateProcessingNotificationAsync(
    ownerId,
    document,
    true,
    cancellationToken);

var resultStatus =
        document.ProcessingStatus ==
            DocumentProcessingStatus.Ready
            ? RetryDocumentProcessingStatus
                .Success
            : RetryDocumentProcessingStatus
                .ProcessingFailed;

    return new RetryDocumentProcessingResult(
        resultStatus,
        Map(document));
}
    public async Task<bool> DeleteAsync(
        Guid ownerId,
        Guid academicProjectId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document =
            await dbContext
                .ProjectDocuments
                .SingleOrDefaultAsync(
                    existingDocument =>
                        existingDocument.Id ==
                            documentId &&
                        existingDocument
                            .AcademicProjectId ==
                            academicProjectId &&
                        existingDocument
                            .AcademicProject
                            .Course
                            .OwnerId ==
                            ownerId,
                    cancellationToken);

        if (document is null)
        {
            return false;
        }

        var storageKey =
            document.StorageKey;

        dbContext.ProjectDocuments.Remove(
            document);

        await dbContext.SaveChangesAsync(
            cancellationToken);

        try
        {
            await fileStorage.DeleteAsync(
                storageKey,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Unable to delete stored document {StorageKey}.",
                storageKey);
        }

        return true;
    }

    private async Task
        ExtractAndSavePagesAsync(
            ProjectDocument document,
            CancellationToken cancellationToken)
    {
        try
        {
            document.ProcessingStatus =
                DocumentProcessingStatus
                    .Extracting;

            await dbContext.SaveChangesAsync(
                cancellationToken);

            await using var pdfStream =
                await fileStorage.OpenReadAsync(
                    document.StorageKey,
                    cancellationToken);

            var extractedPages =
                pdfTextExtractor.Extract(
                    pdfStream);

            foreach (
                var extractedPage
                in extractedPages)
            {
                dbContext.DocumentPages.Add(
                    new DocumentPage
                    {
                        ProjectDocumentId =
                            document.Id,
                        PageNumber =
                            extractedPage
                                .PageNumber,
                        Text =
                            extractedPage.Text
                    });
            }

            document.PageCount =
                extractedPages.Count;

            document.ProcessingStatus =
                DocumentProcessingStatus
                    .Ready;

            document.FailureReason = null;

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            document.ProcessingStatus =
                DocumentProcessingStatus
                    .Failed;

            document.FailureReason =
                Truncate(
                    exception.Message,
                    1000);

            await dbContext.SaveChangesAsync(
                CancellationToken.None);
        }
    }
    private async Task
    CreateProcessingNotificationAsync(
        Guid ownerId,
        ProjectDocument document,
        bool isRetry,
        CancellationToken cancellationToken)
{
    var succeeded =
        document.ProcessingStatus ==
        DocumentProcessingStatus.Ready;

    var type =
        succeeded
            ? "Success"
            : "Error";

    var title =
        succeeded
            ? isRetry
                ? "PDF reprocessing completed"
                : "PDF processing completed"
            : isRetry
                ? "PDF reprocessing failed"
                : "PDF processing failed";

    var message =
        succeeded
            ? $"“{document.OriginalFileName}” is ready. {document.PageCount} page(s) were extracted."
            : $"UniPilot could not process “{document.OriginalFileName}”. You can retry from the project workspace.";

    await notificationService.CreateAsync(
        ownerId,
        type,
        title,
        message,
        null,
        cancellationToken);
}

    private static string Truncate(
        string value,
        int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..maximumLength];
    }

    private static ProjectDocumentResult Map(
        ProjectDocument document)
    {
        return new ProjectDocumentResult(
            document.Id,
            document.AcademicProjectId,
            document.OriginalFileName,
            document.DocumentType.ToString(),
            document.ProcessingStatus
                .ToString(),
            document.FileSizeBytes,
            document.PageCount,
            document.FailureReason,
            document.UploadedAtUtc);
    }
}