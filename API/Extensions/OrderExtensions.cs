using API.DTOs;
using API.Entities.OrderAggregate;
using Microsoft.EntityFrameworkCore;

namespace API.Extensions
{
    public static class OrderExtensions
    {
        /// Projects a query of orders to a query of OrderDto objects.
        /// <param name="query">The query of orders to be projected.</param>
        /// <returns>A query of OrderDto objects.</returns>
        public static IQueryable<OrderDto> ProjectOrderToOrderDto(this IQueryable<Order> query)
        {
            // Project each order to an OrderDto
            return query.Select(order => new OrderDto
            {
                Id = order.Id,
                BuyerId = order.BuyerId,
                OrderDate = order.OrderDate,
                ShippingAddress = order.ShippingAddress,
                DeliveryFee = order.DeliveryFee,
                Subtotal = order.Subtotal,
                OrderStatus = order.OrderStatus.ToString(),
                Total = order.GetTotal(),

                // Project OrderItems within the order to OrderItemDto objects
                OrderItems = order.OrderItems.Select(item => new OrderItemDto
                {
                    ProductId = item.ItemOrdered.ProductId,
                    Name = item.ItemOrdered.Name,
                    PictureUrl = item.ItemOrdered.PictureUrl,
                    Price = item.Price,
                    Quantity = item.Quantity
                }).ToList()
            }).AsNoTracking();
        }
    }
}
