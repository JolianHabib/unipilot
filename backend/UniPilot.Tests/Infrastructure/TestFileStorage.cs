using System.Collections.Concurrent;
using UniPilot.Application.Documents;

namespace UniPilot.Tests.Infrastructure;

public sealed class TestFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _files =
        new();

    public async Task SaveAsync(
        string storageKey,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        await using var memory = new MemoryStream();

        await content.CopyToAsync(memory, cancellationToken);

        if (!_files.TryAdd(storageKey, memory.ToArray()))
        {
            throw new IOException("The file already exists.");
        }
    }

    public Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(storageKey, out var bytes))
        {
            throw new FileNotFoundException();
        }

        Stream stream = new MemoryStream(bytes);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        _files.TryRemove(storageKey, out _);

        return Task.CompletedTask;
    }
}