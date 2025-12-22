using ApartmentRental.Hubs;
using ApartmentRental.Services.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ApartmentRental.Controllers.Api;

[ApiController]
[Route("api/realtime")]
public sealed class RealtimeDemoController : ControllerBase
{
    private readonly IDemoChangeStore _store;
    private readonly IHubContext<DemoHub> _hub;
    private readonly IWebHostEnvironment _env;

    public RealtimeDemoController(
        IDemoChangeStore store,
        IHubContext<DemoHub> hub,
        IWebHostEnvironment env)
    {
        _store = store;
        _hub = hub;
        _env = env;
    }

    // GET /api/realtime/state
    [HttpGet("state")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult GetState()
    {
        var s = _store.Get();
        return Ok(new { version = s.Version, updatedAtUtc = s.UpdatedAtUtc });
    }

    // GET /api/realtime/longpoll?since=10&timeoutMs=25000
    [HttpGet("longpoll")]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> LongPoll([FromQuery] int since = 0, [FromQuery] int timeoutMs = 25000, CancellationToken ct = default)
    {
        if (timeoutMs < 1000) timeoutMs = 1000;
        if (timeoutMs > 60000) timeoutMs = 60000;

        var result = await _store.WaitForChangeAsync(since, TimeSpan.FromMilliseconds(timeoutMs), ct);

        if (result is null)
            return NoContent();

        return Ok(new { version = result.Version, updatedAtUtc = result.UpdatedAtUtc });
    }

    // POST /api/realtime/trigger (dev only)
    [HttpPost("trigger")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Trigger(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var s = _store.Trigger();

        await _hub.Clients.All.SendAsync("demoChanged", new
        {
            version = s.Version,
            updatedAtUtc = s.UpdatedAtUtc
        }, ct);

        return Ok(new { version = s.Version, updatedAtUtc = s.UpdatedAtUtc });
    }

    // POST /api/realtime/reset (dev only)
    [HttpPost("reset")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Reset(CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            return NotFound();

        var s = _store.Reset();

        await _hub.Clients.All.SendAsync("demoChanged", new
        {
            version = s.Version,
            updatedAtUtc = s.UpdatedAtUtc
        }, ct);

        return Ok(new { version = s.Version, updatedAtUtc = s.UpdatedAtUtc });
    }
}
