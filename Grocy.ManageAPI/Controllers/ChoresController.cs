using Grocy.ManageAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Grocy.ManageAPI.Controllers
{
    [Route("api/chores")]
    [ApiController]
    public class ChoresController : ControllerBase
    {
        public ChoresController(CategoryChoreManager manager)
        {
            Manager = manager;
        }

        private CategoryChoreManager Manager { get; set; }

        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleChores()
        {
            await Manager.ScheduleCategory("Städning", 1);
            await Manager.ScheduleCategory("Tvätt", 2);

            return Ok();
        }
    }
}
