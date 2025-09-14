using Microsoft.AspNetCore.Mvc;

namespace ErrorSummarizer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ErrorDemoController : ControllerBase
{
    [HttpGet("nullref")]
    public IActionResult NullRef()
    {
        string? x = null;
        _ = x!.Length; // Force NullReferenceException
        return Ok();
    }

    [HttpGet("arg")]
    public IActionResult Arg()
    {
        throw new ArgumentException("Parameter 'id' must be positive but was -1");
    }

    [HttpGet("invalidop")]
    public IActionResult InvalidOp()
    {
        throw new InvalidOperationException("Operation cannot be performed in current state.");
    }

    [HttpGet("wrap")]
    public IActionResult Wrapped()
    {
        try
        {
            throw new TimeoutException("Waited 30s for DB");
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Higher level failure while processing request", ex);
        }
    }
}