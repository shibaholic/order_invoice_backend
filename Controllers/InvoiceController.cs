using HospitalSupply.Entities;
using HospitalSupply.Repositories;
using HospitalSupply.Services;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSupply.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IItemOrderRepository _itemOrderRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUiPathApiClient _apiClient;

    public InvoiceController(IInvoiceRepository invoiceRepository, IUiPathApiClient apiClient, IItemOrderRepository itemOrderRepository, IUnitOfWork unitOfWork, IOrderRepository orderRepository)
    {
        _invoiceRepository = invoiceRepository;
        _apiClient = apiClient;
        _itemOrderRepository = itemOrderRepository;
        _unitOfWork = unitOfWork;
        _orderRepository = orderRepository;
    }

    public record CreateInvoiceRequest
    {
        public IFormFile File { get; set; }
    }

    [HttpGet]
    [Route("/api/Invoices")]
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
        
        return File(result.FileData, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", result.FileName);
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromForm] CreateInvoiceRequest request)
    {
        if (request.File == null! || request.File.Length == 0)
            return BadRequest("No file was uploaded.");

        if (request.File.ContentType != "application/vnd.openxmlformats-officedocument.wordprocessingml.document") return BadRequest("File must be docx.");

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

    public record ItemOrderRequest2
    {
        public string ItemName { get; init; }
        public int Quantity { get; init; }
        public string? CurrencyAmount { get; init; }
        public string? CurrencyCode { get; init; }
    }
    
    public record UpdateInvoiceRequest
    {
        public List<ItemOrderRequest2> ItemOrders { get; init; }
    }
    
    [HttpPut("{invoiceId}")]
    public async Task<IActionResult> UpdateInvoiceItemOrders([FromRoute] Guid invoiceId, [FromBody] UpdateInvoiceRequest request)
    {
        var invoice = await _invoiceRepository.GetInvoiceAsync(invoiceId);
        if(invoice == null) return NotFound();
        if (invoice.Linked) return BadRequest("Invoice is already linked to an Order.");
        
        invoice.Scanned = true;
        
        var invoiceItemOrders = new List<ItemOrder>();
        foreach (var item in request.ItemOrders)
        {
            invoiceItemOrders.Add(new ItemOrder
            {
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                CurrencyAmount = item.CurrencyAmount,
                CurrencyCode = item.CurrencyCode,
                InvoiceId = invoiceId
            });
        }
        
        // determine if the order's itemOrders matches the invoice's itemOrders
        var orders = await _orderRepository.GetAllOrders();
        orders = orders.OrderByDescending(order => order.DateCreated).ToList();
        var recentOrder = orders.FirstOrDefault();
        if(recentOrder == null) return BadRequest("No order was found.");
        if (recentOrder.InvoiceId != null) return BadRequest("Most recent Order is already linked.");
        
        // match
        var invoiceItemOrdersC = invoiceItemOrders.Count;
        var recentOrderItemOrdersC = recentOrder.ItemOrders.Count;
        
        // assume not linked
        invoice.Linked = false;
        
        var discrepancy = false;
        if (recentOrderItemOrdersC == invoiceItemOrdersC)
        {
            for (int i = 0; i < recentOrderItemOrdersC; i++)
            {
                var oItemOrder = recentOrder.ItemOrders[i];
                var iItemORder = invoiceItemOrders[i];
                Console.WriteLine($"invoice: {iItemORder.ItemName} - {iItemORder.Quantity} - {iItemORder.CurrencyAmount} - {iItemORder.CurrencyCode}");
                Console.WriteLine($"order  : {oItemOrder.ItemName} - {oItemOrder.Quantity} - {oItemOrder.CurrencyAmount} - {oItemOrder.CurrencyCode}");
                if (!(oItemOrder.ItemName == iItemORder.ItemName
                      && oItemOrder.Quantity == iItemORder.Quantity
                      && oItemOrder.CurrencyAmount == iItemORder.CurrencyAmount
                      && oItemOrder.CurrencyCode == iItemORder.CurrencyCode))
                {
                    discrepancy = true;
                    break;
                }
            }
        }
        else
        {
            discrepancy = true;
        }
        
        if (!discrepancy)
        {
            invoice.Linked = true;
            recentOrder.InvoiceId = invoice.Id;
            Console.WriteLine("No discrepancy");
        }
        else
        {
            Console.WriteLine("Discrepancy found");
        }
        
        _unitOfWork.BeginTransaction();
        
        var invoiceResult = await _invoiceRepository.UpdateAsync(invoice);
        if(invoiceResult != 1) return StatusCode(500, "Server Error with Invoice.");
        
        var itemOrdersResult = await _itemOrderRepository.AddItemOrders(invoiceItemOrders);
        if(itemOrdersResult != request.ItemOrders.Count) return StatusCode(500, "Server Error with ItemOrders.");

        if (!discrepancy)
        {
            // update Order
            var orderResult = await _orderRepository.UpdateAsync(recentOrder);
            if(orderResult != 1) return StatusCode(500, "Server Error with Order.");
        }
        
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