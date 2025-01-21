using HospitalSupply.Entities;
using HospitalSupply.Repositories;
using HospitalSupply.Services;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSupply.Controllers;

[ApiController]
[Route("[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IItemOrderRepository _itemOrderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUiPathApiClient _apiClient;

    public InvoiceController(IInvoiceRepository invoiceRepository, IUiPathApiClient apiClient, IItemOrderRepository itemOrderRepository, IUnitOfWork unitOfWork)
    {
        _invoiceRepository = invoiceRepository;
        _apiClient = apiClient;
        _itemOrderRepository = itemOrderRepository;
        _unitOfWork = unitOfWork;
    }

    public record CreateInvoiceRequest
    {
        public IFormFile File { get; set; }
    }

    [HttpGet]
    [Route("/Invoices")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _invoiceRepository.GetInvoiceDtosAsync();
        
        return Ok(result);
    }

    [HttpGet]
    [Route("{invoiceId}")]
    public async Task<IActionResult> GetInvoiceById([FromRoute] Guid invoiceId)
    {
        var result = await _invoiceRepository.GetInvoiceAsync(invoiceId);
        if(result == null) return NotFound();

        result.FileData = null;
        
        return Ok(result);
    }
    
    [HttpGet]
    [Route("file/{invoiceId}")]
    public async Task<IActionResult> GetInvoiceFileById([FromRoute] Guid invoiceId)
    {
        var result = await _invoiceRepository.GetInvoiceAsync(invoiceId);
        if(result == null) return NotFound();
        
        return File(result.FileData, "application/pdf", result.FileName);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromForm] CreateInvoiceRequest request)
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
                DateCreated = DateTime.Now,
                Scanned = false,
                Linked = false
            };

            var result = await _invoiceRepository.CreateAsync(invoice);
            Console.WriteLine($"Invoice Created result: {result}");

            if (result != 1) return StatusCode(500);

            // send request to UiPath Robot to begin it's OrderInvoiceCheck.
            await _apiClient.StartOrderInvoiceCheck();
            
            var returnInvoice = invoice;
            returnInvoice.FileData = null!;

            return Ok(returnInvoice);
        }
        catch (Exception e)
        {
            return StatusCode(500, "Unexpected server error.");
        }
    }

    public record ItemOrderRequests
    {
        public string ItemName { get; init; }
        public int Quantity { get; init; }
        public decimal? CurrencyAmount { get; init; }
        public string? CurrencyCode { get; init; }
    }
    
    public record UpdateInvoiceRequest
    {
        public List<ItemOrderRequests> ItemOrders { get; init; }
        public bool Linked { get; init; }
    }
    
    [HttpPut("{invoiceId}")]
    public async Task<IActionResult> UpdateInvoiceItemOrders([FromRoute] Guid invoiceId, [FromBody] UpdateInvoiceRequest request)
    {
        var invoice = await _invoiceRepository.GetInvoiceAsync(invoiceId);
        if(invoice == null) return NotFound();
        if (invoice.Linked) return BadRequest("Invoice is already linked to an Order.");
        
        var itemOrders = new List<ItemOrder>();
        foreach (var item in request.ItemOrders)
        {
            itemOrders.Add(new ItemOrder
            {
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                CurrencyAmount = item.CurrencyAmount,
                CurrencyCode = item.CurrencyCode,
                InvoiceId = invoiceId
            });
        }

        invoice.Scanned = true;
        invoice.Linked = request.Linked;

        _unitOfWork.BeginTransaction();
        
        var invoiceResult = await _invoiceRepository.UpdateAsync(invoice);
        if(invoiceResult != 1) return StatusCode(500, "Server Error with Invoice.");
        
        var itemOrdersResult = await _itemOrderRepository.AddItemOrders(itemOrders);
        if(itemOrdersResult != request.ItemOrders.Count) return StatusCode(500, "Server Error with ItemOrders.");

        _unitOfWork.Commit();

        invoice.FileData = null;
        
        return Ok(invoice);
    }
    
    // [HttpPost("trigger-job")]
    // public async Task<IActionResult> TriggerJob()
    // {
    //     var thing = await _apiClient.StartOrderInvoiceCheck();
    //     return Ok();
    // }
}