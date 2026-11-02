using System.Collections.Concurrent;
using System.Text.Json;
using SiteSearch.Api.Models;

namespace SiteSearch.Api.Storage;

public interface IDocumentStore
{
    void Upsert(Document document);
    void BulkUpsert(IEnumerable<Document> documents);
    Document? GetById(string id);
    IReadOnlyList<Document> GetAll();
    int Count { get; }
    void SaveToDisk();
    void LoadFromDisk();
}

public class DocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<string, Document> _documents = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _storagePath;
    private readonly ILogger<DocumentStore> _logger;
    private readonly object _diskLock = new();

    public DocumentStore(string storagePath, ILogger<DocumentStore> logger)
    {
        _storagePath = storagePath;
        _logger = logger;
        LoadFromDisk();
    }

    public int Count => _documents.Count;

    public void Upsert(Document document)
    {
        if (string.IsNullOrWhiteSpace(document.Id))
            throw new ArgumentException("Document Id cannot be empty", nameof(document));

        _documents[document.Id] = document;
    }

    public void BulkUpsert(IEnumerable<Document> documents)
    {
        foreach (var doc in documents)
        {
            if (!string.IsNullOrWhiteSpace(doc.Id))
            {
                _documents[doc.Id] = doc;
            }
        }
        SaveToDisk();
    }

    public Document? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        _documents.TryGetValue(id, out var doc);
        return doc;
    }

    public IReadOnlyList<Document> GetAll()
    {
        return _documents.Values.ToList();
    }

    public void SaveToDisk()
    {
        lock (_diskLock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_storagePath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(_documents.Values, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_storagePath, json);
                _logger.LogInformation("Persisted {Count} documents to disk at {Path}", _documents.Count, _storagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist document store to disk at {Path}", _storagePath);
            }
        }
    }

    public void LoadFromDisk()
    {
        lock (_diskLock)
        {
            try
            {
                if (File.Exists(_storagePath))
                {
                    var json = File.ReadAllText(_storagePath);
                    var docs = JsonSerializer.Deserialize<List<Document>>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (docs != null)
                    {
                        _documents.Clear();
                        foreach (var doc in docs)
                        {
                            if (!string.IsNullOrWhiteSpace(doc.Id))
                            {
                                _documents[doc.Id] = doc;
                            }
                        }
                        _logger.LogInformation("Loaded {Count} documents from disk store at {Path}", _documents.Count, _storagePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load document store from disk at {Path}", _storagePath);
            }
        }
    }
}
