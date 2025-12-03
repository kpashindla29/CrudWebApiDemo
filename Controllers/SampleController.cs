using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Roles = "Admin")]
     [ApiController]
     [Route("api/[controller]")]
     public class SampleController : ControllerBase
     {
         [HttpGet]
         public IActionResult Get()
         {
             return Ok("This is a protected endpoint accessible only to Admins.");
         }
     }