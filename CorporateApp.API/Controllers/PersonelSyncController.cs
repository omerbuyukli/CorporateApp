using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CorporateApp.Application.Interfaces;

namespace CorporateApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class PersonelSyncController : ControllerBase
    {
        private readonly IPersonelSyncJobService _jobService;

        public PersonelSyncController(IPersonelSyncJobService jobService)
        {
            _jobService = jobService;
        }

        [HttpPost("trigger")]
        [Authorize]
        public IActionResult TriggerSync()
        {



            _jobService.TriggerImmediateSync();
            return Ok(new { message = "Sync job triggered successfully" });
        }
    }
}
