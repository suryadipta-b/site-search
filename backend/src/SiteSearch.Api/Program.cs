using SiteSearch.Api.Services;
using SiteSearch.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure File Paths
var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dataDir))
{
    var projectData = Path.Combine(Directory.GetCurrentDirectory(), "data");
    var parentData = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data");
    if (Directory.Exists(projectData)) dataDir = projectData;
    else if (Directory.Exists(parentData)) dataDir = Path.GetFullPath(parentData);
}

var synonymsPath = Path.Combine(dataDir, "synonyms.json");
var storePath = Path.Combine(dataDir, "store.json");

// Register Singletons
builder.Services.AddSingleton<ISynonymService>(sp => new SynonymService(synonymsPath));
builder.Services.AddSingleton<IQueryBuilder, QueryBuilder>();
builder.Services.AddSingleton<IHighlighter, Highlighter>();
builder.Services.AddSingleton<IDocumentStore>(sp =>
    new DocumentStore(storePath, sp.GetRequiredService<ILogger<DocumentStore>>()));
builder.Services.AddSingleton<ISearchEngine, SearchEngine>();
builder.Services.AddSingleton<IIngestionService, IngestionService>();

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthorization();
app.MapControllers();

// Auto-run ingestion on startup if store is empty
using (var scope = app.Services.CreateScope())
{
    var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();
    if (store.Count == 0)
    {
        var ingestion = scope.ServiceProvider.GetRequiredService<IIngestionService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Store is empty on startup. Triggering initial ingestion...");
        ingestion.IngestAll();
    }
}

app.Run();
