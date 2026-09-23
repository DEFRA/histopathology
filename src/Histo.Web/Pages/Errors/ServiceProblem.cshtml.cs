using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Histo.Web.Pages.Errors;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ServiceProblemModel : PageModel
{
    private readonly ILogger<ServiceProblemModel> _logger;

    public ServiceProblemModel(ILogger<ServiceProblemModel> logger) => _logger = logger;

    public void OnGet()
    {
        // Log opaque TraceId only — never log exception details or PII to the page
        var traceId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        _logger.LogError("Service problem page shown. TraceId: {TraceId}", traceId);
    }
}
