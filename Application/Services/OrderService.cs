using Application.DTOs;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IOrderService
    {
        Task<OrderDto> GetOrderAsync(Guid id);
        Task<OrderDto> GetOrderByNumberAsync(string orderNumber);
        Task<IEnumerable<OrderDto>> GetOrdersByCustomerAsync(Guid customerId);
        Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, Guid? customerId = null);
        Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status);
        Task<OrderDto> CancelOrderAsync(Guid orderId, string reason = null);
        Task<IEnumerable<OrderDto>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<OrderDto> CreateOrderFromCartAsync(Guid? userId = null, string sessionId = null, CreateOrderDto? additionalInfo = null);
    }

    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartService _cartService;
        private readonly UserManager<User> _userManager;
        private readonly IProductService _productService;

        public OrderService(
            IUnitOfWork unitOfWork,
            ICartService cartService,
            UserManager<User> userManager,
            IProductService productService)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
            _userManager = userManager;
            _productService = productService;
        }

        public async Task<OrderDto> GetOrderAsync(Guid id)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(id);
            if (order == null)
            {
                throw new NotFoundException("Order", id);
            }

            return MapOrderToDto(order);
        }

        public async Task<OrderDto> GetOrderByNumberAsync(string orderNumber)
        {
            var order = await _unitOfWork.Orders.GetByOrderNumberAsync(orderNumber);
            if (order == null)
            {
                throw new NotFoundException("Order", orderNumber);
            }

            return MapOrderToDto(order);
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersByCustomerAsync(Guid customerId)
        {
            var orders = await _unitOfWork.Orders.GetByCustomerIdAsync(customerId);
            return orders.Select(MapOrderToDto);
        }

        public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, Guid? customerId = null)
        {
            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Validate items
                if (dto.Items == null || !dto.Items.Any())
                {
                    throw new ValidationException("Order must contain at least one item.");
                }

                User? customer = null;
                if (customerId.HasValue)
                {
                    customer = await _userManager.FindByIdAsync(customerId.Value.ToString());
                    if (customer == null)
                    {
                        throw new NotFoundException("Customer", customerId.Value);
                    }
                }

                // Create order
                var order = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    CustomerId = customerId ?? Guid.Empty,
                    Customer = customer,
                    CustomerEmail = dto.CustomerEmail ?? customer?.Email,
                    CustomerPhone = dto.CustomerPhone ?? customer?.PhoneNumber,
                    CustomerName = dto.CustomerName ?? customer?.FullName,
                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.Pending,
                    FulfillmentStatus = FulfillmentStatus.Unfulfilled,
                    CreatedAt = DateTime.UtcNow
                };

                // Process order items
                decimal subtotal = 0;
                var orderItems = new List<OrderItem>();

                foreach (var itemDto in dto.Items)
                {
                    var product = await _unitOfWork.Products.GetByIdAsync(itemDto.ProductId);
                    if (product == null || product.IsDeleted || product.Status != ProductStatus.Published)
                    {
                        throw new NotFoundException("Product", itemDto.ProductId);
                    }

                    // Check availability
                    var isAvailable = await _productService.IsProductAvailableAsync(itemDto.ProductId, itemDto.Quantity);
                    if (!isAvailable)
                    {
                        throw new ValidationException($"Product '{product.Title}' is not available in the requested quantity.");
                    }

                    var unitPrice = product.DiscountPrice ?? product.Price;
                    var totalPrice = unitPrice * itemDto.Quantity;

                    var orderItem = new OrderItem
                    {
                        ProductId = itemDto.ProductId,
                        Product = product,
                        Quantity = itemDto.Quantity,
                        UnitPrice = unitPrice,
                        DiscountPrice = product.DiscountPrice,
                        CreatedAt = DateTime.UtcNow
                    };

                    orderItems.Add(orderItem);
                    subtotal += totalPrice;

                    // Update product stock for physical products
                    if (product.Format == ProductFormat.Print)
                    {
                        await _productService.UpdateStockAsync(itemDto.ProductId, -itemDto.Quantity);
                    }
                }

                // Calculate totals
                order.Subtotal = subtotal;
                order.TaxAmount = CalculateTax(subtotal);
                order.DiscountAmount = 0; // Could apply discounts here
                order.TotalAmount = order.Subtotal + order.TaxAmount - order.DiscountAmount;

                // Add order items
                foreach (var item in orderItems)
                {
                    order.OrderItems.Add(item);
                }

                // Save order
                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Create initial payment record if payment method specified
                if (dto.PaymentMethod != PaymentMethod.Manual)
                {
                    var payment = new Payment
                    {
                        PaymentReference = GeneratePaymentReference(),
                        OrderId = order.Id,
                        Order = order,
                        CustomerId = customerId,
                        Customer = customer,
                        Method = dto.PaymentMethod,
                        Type = dto.PaymentType,
                        Status = PaymentStatus.Pending,
                        Amount = order.TotalAmount,
                        Currency = "PKR",
                        CreatedAt = DateTime.UtcNow
                    };

                    // Handle offline payment details
                    if (dto.PaymentType == PaymentType.Offline)
                    {
                        payment.BankName = dto.BankName;
                        payment.AccountNumber = dto.AccountNumber;
                        payment.TransactionId = dto.TransactionId;
                        // Deposit slip would be handled separately via file upload
                    }

                    await _unitOfWork.Payments.AddAsync(payment);
                    await _unitOfWork.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return await GetOrderAsync(order.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<OrderDto> CreateOrderFromCartAsync(Guid? userId = null, string sessionId = null, CreateOrderDto? additionalInfo = null)
        {
            // Get cart
            var cart = await _cartService.GetCartAsync(userId, sessionId);

            if (cart.TotalItems == 0)
            {
                throw new ValidationException("Cart is empty.");
            }

            // Create order DTO from cart
            var orderDto = additionalInfo ?? new CreateOrderDto();

            orderDto.Items = cart.Items.Select(item => new OrderItemRequestDto
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity
            }).ToList();

            // Set customer info if available
            if (userId.HasValue)
            {
                var user = await _userManager.FindByIdAsync(userId.Value.ToString());
                if (user != null)
                {
                    orderDto.CustomerEmail = orderDto.CustomerEmail ?? user.Email;
                    orderDto.CustomerPhone = orderDto.CustomerPhone ?? user.PhoneNumber;
                    orderDto.CustomerName = orderDto.CustomerName ?? user.FullName;
                }
            }

            // Create order
            var order = await CreateOrderAsync(orderDto, userId);

            // Clear cart after successful order
            await _cartService.ClearCartAsync(userId, sessionId);

            return order;
        }

        public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new NotFoundException("Order", orderId);
            }

            // Validate status transition
            ValidateStatusTransition(order.Status, status);

            order.Status = status;

            if (status == OrderStatus.Completed)
            {
                order.CompletedAt = DateTime.UtcNow;
                order.FulfillmentStatus = FulfillmentStatus.Fulfilled;
            }
            else if (status == OrderStatus.Cancelled)
            {
                order.CancelledAt = DateTime.UtcNow;

                // Restore product stock
                await RestoreProductStockAsync(order);
            }

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            return await GetOrderAsync(orderId);
        }

        public async Task<OrderDto> CancelOrderAsync(Guid orderId, string reason = null)
        {
            return await UpdateOrderStatusAsync(orderId, OrderStatus.Cancelled);
        }

        public async Task<IEnumerable<OrderDto>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            var orders = await _unitOfWork.Orders.GetOrdersByDateRangeAsync(startDate, endDate);
            return orders.Select(MapOrderToDto);
        }

        private async Task RestoreProductStockAsync(Order order)
        {
            foreach (var item in order.OrderItems)
            {
                if (item.Product.Format == ProductFormat.Print)
                {
                    await _productService.UpdateStockAsync(item.ProductId, item.Quantity);
                }
            }
        }

        private void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
        {
            // Define valid status transitions
            var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
            {
                [OrderStatus.Pending] = new() { OrderStatus.Processing, OrderStatus.Cancelled },
                [OrderStatus.Processing] = new() { OrderStatus.Completed, OrderStatus.Cancelled },
                [OrderStatus.Completed] = new() { OrderStatus.Refunded },
                [OrderStatus.Cancelled] = new() { },
                [OrderStatus.Refunded] = new() { }
            };

            if (!validTransitions[currentStatus].Contains(newStatus))
            {
                throw new ValidationException($"Invalid status transition from {currentStatus} to {newStatus}");
            }
        }

        private decimal CalculateTax(decimal amount)
        {
            // 15% tax rate - adjust as needed
            return amount * 0.15m;
        }

        private string GenerateOrderNumber()
        {
            return $"MENT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        }

        private string GeneratePaymentReference()
        {
            return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }

        private OrderDto MapOrderToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.CustomerName ?? order.Customer?.FullName,
                CustomerEmail = order.CustomerEmail ?? order.Customer?.Email,
                CustomerPhone = order.CustomerPhone ?? order.Customer?.PhoneNumber,
                Subtotal = order.Subtotal,
                TaxAmount = order.TaxAmount,
                DiscountAmount = order.DiscountAmount,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                PaymentStatus = order.PaymentStatus.ToString(),
                FulfillmentStatus = order.FulfillmentStatus.ToString(),
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                CompletedAt = order.CompletedAt,
                CancelledAt = order.CancelledAt
            };
        }
    }
}
