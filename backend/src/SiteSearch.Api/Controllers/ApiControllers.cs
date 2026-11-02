using Microsoft.AspNetCore.Mvc;
using SiteSearch.Api.Models;
using SiteSearch.Api.Services;
using SiteSearch.Api.Storage;

namespace SiteSearch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly ISearchEngine _searchEngine;

    public SearchController(ISearchEngine searchEngine)
    {
        _searchEngine = searchEngine;
    }

    [HttpGet]
    public ActionResult<SearchResponse> Search(
        [FromQuery] string? q,
        [FromQuery] string? type,
        [FromQuery] string? project,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery(Name = "as")] string? asUser = null)
    {
        // Support either ?as=asha or X-User: asha header
        if (string.IsNullOrWhiteSpace(asUser) && Request.Headers.TryGetValue("X-User", out var headerUser))
        {
            asUser = headerUser.ToString();
        }

        var request = new SearchRequest
        {
            Q = q,
            Type = type,
            Project = project,
            Page = page,
            PageSize = pageSize,
            AsUser = asUser
        };

        var response = _searchEngine.Search(request);
        return Ok(response);
    }
}

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentStore _documentStore;

    public DocumentsController(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    [HttpGet("{id}")]
    public ActionResult<Document> GetById(string id, [FromQuery(Name = "as")] string? asUser = null)
    {
        if (string.IsNullOrWhiteSpace(asUser) && Request.Headers.TryGetValue("X-User", out var headerUser))
        {
            asUser = headerUser.ToString();
        }

        var user = HardcodedUsers.GetUserOrDefault(asUser);
        var doc = _documentStore.GetById(id);

        // Security check: if document does not exist, OR user does not belong to project,
        // return 404 to avoid leaking document existence!
        if (doc == null || !user.CanAccessProject(doc.ProjectId))
        {
            return NotFound(new { message = $"Document '{id}' not found." });
        }

        return Ok(doc);
    }
}

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<object>> GetUsers()
    {
        var users = HardcodedUsers.All.Values.Select(u => new
        {
            username = u.Username,
            projectMemberships = u.ProjectMemberships,
            description = u.Username switch
            {
                "asha" => "Asha · Project P1 member",
                "ravi" => "Ravi · Project P2 member",
                "meera" => "Meera · Projects P1 & P2 member",
                _ => "Guest · Public regulations only"
            }
        });

        return Ok(users);
    }
}

[ApiController]
[Route("api/[controller]")]
public class IngestionController : ControllerBase
{
    private readonly IIngestionService _ingestionService;

    public IngestionController(IIngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    [HttpPost]
    public ActionResult<IngestionSummary> Ingest()
    {
        var result = _ingestionService.IngestAll();
        return Ok(result);
    }
}
