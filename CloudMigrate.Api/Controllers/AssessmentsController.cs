
using CloudMigrate.Business.Services.Interface;
using CloudMigrate.DomainObjects.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace CloudMigrate.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class AssessmentsController(
        IAssessmentService _assessmentService
    ) : ControllerBase
    {
        
        [HttpPost]
        public async Task<IActionResult> CreateAssessment([FromBody] AssessmentRequest request)
        {
            var response = await _assessmentService.GenerateAssessment(request);
            return Ok(response);
        }
    }
}