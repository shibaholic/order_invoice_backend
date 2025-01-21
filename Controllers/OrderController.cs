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
        public List<ItemOrderRequest> ItemOrders { get; init; }
    }

    public record ItemOrderRequest
    {
        public int Serial { get; init; }
        public string ItemName { get; init; }
        public int Quantity { get; init; }
        public decimal? CurrencyAmount { get; init; }
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
                OrderId = order.Id
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
        
        return Ok();
    }
}