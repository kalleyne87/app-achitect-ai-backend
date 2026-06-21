
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
        public IActionResult CreateAssessment([FromBody] AssessmentRequest request)
        {

            var response = _assessmentService.GenerateAssessment(request);

            return Ok(response);
        }
    }
}