using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSupply.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    [HttpGet("docx-file")]
    public async Task<IActionResult> GetDocxFile()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files", "faktura.docx");

        // Check if file exists
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();  // Return 404 if the file doesn't exist
        }

        var fileBytes = System.IO.File.ReadAllBytes(filePath);
        return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "faktura.docx");
    }
}