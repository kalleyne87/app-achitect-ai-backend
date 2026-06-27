using ArchitectAI.Business.Services.Interface;
using ArchitectAI.DomainObjects.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ArchitectAI.Api.Controllers
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

        [HttpPost("answers")]
        public async Task<IActionResult> SubmitAnswers([FromBody] SubmitAnswersRequest request)
        {
            try
            {
                var response = await _assessmentService.SubmitAnswers(request);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}