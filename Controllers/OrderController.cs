using System.Collections.Specialized;
using HospitalSupply.Entities;
using HospitalSupply.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSupply.Controllers;

[ApiController]
[Route("[controller]")]
public class OrderController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IItemOrderRepository _itemOrderRepository;
    private readonly IUnitOfWork _unitOfWork;

    public OrderController(IOrderRepository orderRepository, IItemOrderRepository itemOrderRepository, IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _itemOrderRepository = itemOrderRepository;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _orderRepository.GetAllOrders();
        
        return Ok(orders);
    }

    public record CreateOrderRequest
    {
        public string SupplierName { get; init; }
        public List<ItemOrderRequest1> ItemOrders { get; init; }
    }

    public record ItemOrderRequest1
    {
        public int Serial { get; init; }
        public string ItemName { get; init; }
        public int Quantity { get; init; }
        public string? CurrencyAmount { get; init; }
        public string? CurrencyCode { get; init; }
    }
    
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        // create Order from request
        Order order = new Order { Id = Guid.NewGuid(), SupplierName = request.SupplierName};
        
        var itemOrders = new List<ItemOrder>();
        foreach (var itemOrderRequest in request.ItemOrders)
        {
            itemOrders.Add(new ItemOrder {
                ItemName = itemOrderRequest.ItemName, 
                Quantity = itemOrderRequest.Quantity,
                CurrencyAmount = itemOrderRequest.CurrencyAmount,
                CurrencyCode = itemOrderRequest.CurrencyCode,
                OrderId = order.Id,
                InvoiceId = null
            });
        }
        order.ItemOrders = itemOrders;
        
        // start transaction
        _unitOfWork.BeginTransaction();
        
        var orderResult = await _orderRepository.AddOrder(order);
        Console.WriteLine($"Order Create result: {orderResult}");
        
        if(orderResult != 1) return StatusCode(500);
        
        // create ItemOrders
        var itemOrdersResult = await _itemOrderRepository.AddItemOrders(order.ItemOrders);
        Console.WriteLine($"ItemOrder Create result: {itemOrdersResult}");
        
        if(itemOrdersResult != order.ItemOrders.Count) return StatusCode(500);
        
        _unitOfWork.Commit();
        
        return Ok(order);
    }

    public record UpdateOrderRequest
    {
        public Guid InvoiceId { get; init; }
    }

    [HttpPut("{orderId}")]
    public async Task<IActionResult> Update([FromRoute] Guid orderId, [FromBody] UpdateOrderRequest request)
    {
        var order = await _orderRepository.GetOrder(orderId);
        if(order == null) return NotFound();
        if (order.InvoiceId != null) return BadRequest("Order is already linked to an Invoice");

        if(request.InvoiceId == null) return BadRequest();
        order.InvoiceId = request.InvoiceId;

        try
        {
            var result = await _orderRepository.UpdateAsync(order);
            if (result != 1) return StatusCode(500);
        }
        catch (Exception e)
        {
            return StatusCode(500, "Server Error with Order.");
        }

        return Ok(order);
    }
}