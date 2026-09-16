using Microsoft.Extensions.Hosting;
using UniPilot.Application.Documents;

namespace UniPilot.Infrastructure.Documents;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _rootPath;

    public LocalFileStorage(IHostEnvironment environment)
    {
        _rootPath = Path.Combine(
            environment.ContentRootPath,
            "storage",
            "project-documents");
    }

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);

        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException(
                "The storage directory could not be resolved.");

        Directory.CreateDirectory(directory);

        await using var output = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(
            output,
            cancellationToken);
    }

    public Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveSafePath(string storageKey)
    {
        var normalizedRoot = Path.GetFullPath(_rootPath);

        var fullPath = Path.GetFullPath(
            Path.Combine(normalizedRoot, storageKey));

        var requiredPrefix =
            normalizedRoot.TrimEnd(
                Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
                requiredPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The storage key contains an invalid path.");
        }

        return fullPath;
    }
}