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
        [HttpGet]
        public async Task<IActionResult> GetAssessments()
        {
            var assessments = await _assessmentService.GetAssessments();
            return Ok(assessments);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAssessmentById(int id)
        {
            var assessment = await _assessmentService.GetAssessmentById(id);

            if (assessment == null)
                return NotFound();

            return Ok(assessment);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAssessment([FromBody] AssessmentRequest request)
        {
            var response = await _assessmentService.GenerateAssessment(request);
            return Ok(response);
        }
    }
}