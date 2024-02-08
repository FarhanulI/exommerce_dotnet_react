using API.Data;
using API.DTOs;
using API.Entities;
using API.Entities.OrderAggregate;
using API.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    public class OrdersController : BaseApiController
    {
        private readonly StoreContext _context;

        // Constructor to inject StoreContext
        public OrdersController(StoreContext context)
        {
            _context = context;
        }

        // Action to get all orders for the current user
        [HttpGet]
        public async Task<ActionResult<List<OrderDto>>> GetOrders()
        {
            var orders = await _context.Orders
                                .ProjectOrderToOrderDto() // Extension method to map Order entities to OrderDto
                                .Where(x => x.BuyerId == User.Identity.Name) // Filter orders by current user
                                .ToListAsync();

            return orders;
        }

        // Action to get a specific order for the current user by ID
        [HttpGet("{id}", Name = "GetOrder")]
        public async Task<ActionResult<OrderDto>> GetOrders(int id)
        {
            var orders = await _context.Orders
                                .ProjectOrderToOrderDto() // Extension method to map Order entities to OrderDto
                                .Where(x => x.BuyerId == User.Identity.Name && x.Id == id) // Filter orders by current user and ID
                                .FirstOrDefaultAsync();

            return orders;
        }

        // Action to create a new order
        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(CreateOrderDto orderDto)
        {
            // Retrieve the basket for the current user
            var basket = await _context.Baskets
                        .RetrieveBasketWithItems(User.Identity.Name)
                        .FirstOrDefaultAsync();

            if (basket == null) return BadRequest(new ProblemDetails { Title = "Could not locate basket" });

            // Initialize list to hold order items
            var items = new List<OrderItem>();

            // Loop through basket items to create order items
            foreach (var item in basket.Items)
            {
                var productItem = await _context.Products.FindAsync(item.ProductId);

                // Create ordered item
                var itemOrdered = new ProductItemOrdered
                {
                    ProductId = productItem.Id,
                    Name = productItem.Name,
                    PictureUrl = productItem.PictureUrl
                };

                // Create order item
                var orderItem = new OrderItem
                {
                    ItemOrdered = itemOrdered,
                    Price = productItem.Price,
                    Quantity = item.Quantity
                };

                items.Add(orderItem);

                // Update product quantity in stock
                productItem.QuantityInStock -= item.Quantity;
            }

            // Calculate subtotal
            var subtotal = items.Sum(item => item.Price * item.Quantity);

            // Calculate delivery fee
            var deliveryFee = subtotal > 10000 ? 0 : 500;

            // Create order entity
            var order = new Order
            {
                OrderItems = items,
                BuyerId = User.Identity.Name,
                ShippingAddress = orderDto.ShippingAddress,
                Subtotal = subtotal,
                DeliveryFee = deliveryFee,
                PaymentIntentId = basket.PaymentIntentId
            };

            // Add order to context and remove basket
            _context.Orders.Add(order);
            _context.Baskets.Remove(basket);

            // If specified, save shipping address to user's profile
            if (orderDto.SaveAddress)
            {
                var user = await _context.Users
                    .Include(a => a.Address)
                    .FirstOrDefaultAsync(x => x.UserName == User.Identity.Name);

                var address = new UserAddress
                {
                    FullName = orderDto.ShippingAddress.FullName,
                    Address1 = orderDto.ShippingAddress.Address1,
                    Address2 = orderDto.ShippingAddress.Address2,
                    City = orderDto.ShippingAddress.City,
                    State = orderDto.ShippingAddress.State,
                    Zip = orderDto.ShippingAddress.Zip,
                    Country = orderDto.ShippingAddress.Country
                };
                user.Address = address;
            }

            // Save changes to database
            var result = await _context.SaveChangesAsync() > 0;

            // Return created order if successful, otherwise return BadRequest
            if (result) return CreatedAtRoute("GetOrder", new { id = order.Id }, order.Id);
            return BadRequest("Problem creating order");
        }
    }
}
