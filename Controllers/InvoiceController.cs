using HospitalSupply.Entities;
using HospitalSupply.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSupply.Controllers;

[ApiController]
[Route("[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceRepository _invoiceRepository;

    public InvoiceController(IInvoiceRepository invoiceRepository)
    {
        _invoiceRepository = invoiceRepository;
    }

    public record InvoiceRequest
    {
        public IFormFile File { get; set; }
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromForm] InvoiceRequest request)
    {
        if (request.File == null! || request.File.Length == 0)
            return BadRequest("No file was uploaded.");

        if (request.File.ContentType != "application/pdf") return BadRequest("File must be pdf.");

        try
        {
            byte[] fileData;
            using (var memoryStream = new MemoryStream())
            {
                await request.File.CopyToAsync(memoryStream);
                fileData = memoryStream.ToArray();
            }

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                FileName = request.File.FileName,
                FileData = fileData,
                ContentType = request.File.ContentType,
                DateCreated = DateTime.Now
            };

            var result = await _invoiceRepository.CreateAsync(invoice);
            Console.WriteLine($"Invoice Created result: {result}");

            if (result != 1) return StatusCode(500);

            // TODO: send request to UiPath Robot to begin it's Matching.
            
            var returnInvoice = invoice;
            returnInvoice.FileData = null;

            return Ok(returnInvoice);
        }
        catch (Exception e)
        {
            return StatusCode(500, "Unexpected server error.");
        }
    }
}