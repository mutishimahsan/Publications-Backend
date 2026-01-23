using Application.DTOs;
using Application.Interfaces;
using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaymentMethod = Domain.Enums.PaymentMethod;

namespace Application.Services
{
    public interface IPaymentService
    {
        Task<PaymentDto> GetPaymentAsync(Guid id);
        Task<IEnumerable<PaymentDto>> GetPaymentsByOrderAsync(Guid orderId);
        Task<PaymentDto> ProcessOnlinePaymentAsync(ProcessOnlinePaymentDto dto);
        Task<PaymentDto> ProcessOfflinePaymentAsync(ProcessOfflinePaymentDto dto);
        Task<PaymentDto> ApproveOfflinePaymentAsync(ApproveOfflinePaymentDto dto, string approvedBy);
        Task<IEnumerable<PaymentDto>> GetPendingOfflinePaymentsAsync();
        Task<PaymentSessionDto> CreateStripeCheckoutSessionAsync(Guid orderId, Guid customerId);
        Task<bool> HandleStripeWebhookAsync(string json, string signature);
        Task<PaymentDto> VerifyAndCompletePaymentAsync(string paymentReference);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IOrderService _orderService;
        private readonly IFileStorageService _fileStorageService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(
            IUnitOfWork unitOfWork,
            IOrderService orderService,
            IFileStorageService fileStorageService,
            IConfiguration configuration,
            ILogger<PaymentService> logger)
        {
            _unitOfWork = unitOfWork;
            _orderService = orderService;
            _fileStorageService = fileStorageService;
            _configuration = configuration;
            _logger = logger;

            // Configure Stripe
            StripeConfiguration.ApiKey = _configuration["Stripe:SecretKey"];
        }

        public async Task<PaymentDto> GetPaymentAsync(Guid id)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(id);
            if (payment == null)
            {
                throw new NotFoundException("Payment", id);
            }

            return MapPaymentToDto(payment);
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByOrderAsync(Guid orderId)
        {
            var payments = await _unitOfWork.Payments.GetByOrderIdAsync(orderId);
            return payments.Select(MapPaymentToDto);
        }

        public async Task<PaymentDto> ProcessOnlinePaymentAsync(ProcessOnlinePaymentDto dto)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                throw new NotFoundException("Order", dto.OrderId);
            }

            // Validate order can accept payment
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                throw new ValidationException("Order is already paid.");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                throw new ValidationException("Cannot process payment for cancelled order.");
            }

            // Create payment record
            var payment = new Payment
            {
                PaymentReference = GeneratePaymentReference(),
                OrderId = dto.OrderId,
                Order = order,
                CustomerId = order.CustomerId,
                Method = dto.PaymentMethod,
                Type = PaymentType.Online,
                Status = PaymentStatus.Pending,
                Amount = order.TotalAmount,
                Currency = "PKR",
                CreatedAt = DateTime.UtcNow
            };

            // For Stripe, we'll create a checkout session
            if (dto.PaymentMethod == PaymentMethod.Stripe)
            {
                payment.GatewayTransactionId = $"stripe_session_{Guid.NewGuid()}";
            }
            else if (dto.PaymentMethod == PaymentMethod.PayFast)
            {
                payment.GatewayTransactionId = $"payfast_{Guid.NewGuid()}";
            }

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return MapPaymentToDto(payment);
        }

        public async Task<PaymentDto> ProcessOfflinePaymentAsync(ProcessOfflinePaymentDto dto)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(dto.OrderId);
            if (order == null)
            {
                throw new NotFoundException("Order", dto.OrderId);
            }

            // Validate order can accept payment
            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                throw new ValidationException("Order is already paid.");
            }

            if (order.Status == OrderStatus.Cancelled)
            {
                throw new ValidationException("Cannot process payment for cancelled order.");
            }

            // Save deposit slip if provided
            string? depositSlipUrl = null;
            if (dto.DepositSlip != null)
            {
                depositSlipUrl = await _fileStorageService.SaveFileAsync(
                    dto.DepositSlip,
                    "payment-proofs"
                );
            }

            // Create payment record
            var payment = new Payment
            {
                PaymentReference = GeneratePaymentReference(),
                OrderId = dto.OrderId,
                Order = order,
                CustomerId = order.CustomerId,
                Method = PaymentMethod.BankTransfer,
                Type = PaymentType.Offline,
                Status = PaymentStatus.Pending,
                Amount = order.TotalAmount,
                Currency = "PKR",
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                TransactionId = dto.TransactionId,
                DepositSlipUrl = depositSlipUrl,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            // Update order payment status
            await _unitOfWork.Orders.UpdatePaymentStatusAsync(order.Id, PaymentStatus.Pending);
            await _unitOfWork.SaveChangesAsync();

            return MapPaymentToDto(payment);
        }

        public async Task<PaymentDto> ApproveOfflinePaymentAsync(ApproveOfflinePaymentDto dto, string approvedBy)
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(dto.PaymentId);
            if (payment == null)
            {
                throw new NotFoundException("Payment", dto.PaymentId);
            }

            if (payment.Type != PaymentType.Offline)
            {
                throw new ValidationException("Only offline payments can be approved manually.");
            }

            if (payment.Status == PaymentStatus.Paid)
            {
                throw new ValidationException("Payment is already approved.");
            }

            if (dto.Approve)
            {
                payment.Status = PaymentStatus.Paid;
                payment.ApprovedBy = approvedBy;
                payment.ApprovedAt = DateTime.UtcNow;
                payment.ProcessedAt = DateTime.UtcNow;

                // Update order payment status
                await _unitOfWork.Orders.UpdatePaymentStatusAsync(payment.OrderId, PaymentStatus.Paid);

                // Update order status to processing
                var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
                if (order != null && order.Status == OrderStatus.Pending)
                {
                    await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);
                }
            }
            else
            {
                payment.Status = PaymentStatus.Failed;
                payment.GatewayResponse = dto.Notes;
            }

            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return MapPaymentToDto(payment);
        }

        public async Task<IEnumerable<PaymentDto>> GetPendingOfflinePaymentsAsync()
        {
            var payments = await _unitOfWork.Payments.GetPendingOfflinePaymentsAsync();
            return payments.Select(MapPaymentToDto);
        }

        public async Task<PaymentSessionDto> CreateStripeCheckoutSessionAsync(Guid orderId, Guid customerId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new NotFoundException("Order", orderId);
            }

            // Check if order already has a pending Stripe payment
            var existingPayments = await _unitOfWork.Payments.GetByOrderIdAsync(orderId);
            var existingStripePayment = existingPayments.FirstOrDefault(p =>
                p.Method == PaymentMethod.Stripe &&
                p.Status == PaymentStatus.Pending);

            Payment payment;
            if (existingStripePayment != null)
            {
                payment = existingStripePayment;
            }
            else
            {
                // Create new payment record
                payment = new Payment
                {
                    PaymentReference = GeneratePaymentReference(),
                    OrderId = orderId,
                    CustomerId = customerId,
                    Method = PaymentMethod.Stripe,
                    Type = PaymentType.Online,
                    Status = PaymentStatus.Pending,
                    Amount = order.TotalAmount,
                    Currency = "PKR",
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Payments.AddAsync(payment);
                await _unitOfWork.SaveChangesAsync();
            }

            // Create Stripe checkout session
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "pkr",
                            UnitAmount = Convert.ToInt64(order.TotalAmount * 100), // Convert to cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order #{order.OrderNumber}",
                                Description = $"Payment for order {order.OrderNumber}"
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = $"{_configuration["Stripe:SuccessUrl"]}?session_id={{CHECKOUT_SESSION_ID}}&order_id={orderId}",
                CancelUrl = $"{_configuration["Stripe:CancelUrl"]}?order_id={orderId}",
                ClientReferenceId = payment.PaymentReference,
                Metadata = new Dictionary<string, string>
                {
                    { "order_id", orderId.ToString() },
                    { "payment_id", payment.Id.ToString() },
                    { "customer_id", customerId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            // Update payment with Stripe session ID
            payment.GatewayTransactionId = session.Id;
            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();

            return new PaymentSessionDto
            {
                SessionId = session.Id,
                PaymentId = payment.Id,
                PaymentReference = payment.PaymentReference,
                Url = session.Url,
                ExpiresAt = session.ExpiresAt
            };
        }

        public async Task<bool> HandleStripeWebhookAsync(string json, string signature)
        {
            try
            {
                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                var stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);

                _logger.LogInformation($"Stripe webhook received: {stripeEvent.Type}");

                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        await HandleCheckoutSessionCompleted(stripeEvent);
                        break;

                    case "checkout.session.async_payment_succeeded":
                        await HandleCheckoutSessionAsyncPaymentSucceeded(stripeEvent);
                        break;

                    case "checkout.session.async_payment_failed":
                        await HandleCheckoutSessionAsyncPaymentFailed(stripeEvent);
                        break;

                    case "payment_intent.succeeded":
                        await HandlePaymentIntentSucceeded(stripeEvent);
                        break;

                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentPaymentFailed(stripeEvent);
                        break;
                }

                return true;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook error");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return false;
            }
        }

        private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            await ProcessStripePayment(session.Id, PaymentStatus.Paid);
        }

        private async Task HandleCheckoutSessionAsyncPaymentSucceeded(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            await ProcessStripePayment(session.Id, PaymentStatus.Paid);
        }

        private async Task HandleCheckoutSessionAsyncPaymentFailed(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            await ProcessStripePayment(session.Id, PaymentStatus.Failed);
        }

        private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            // Find payment by payment intent ID
            var payments = await _unitOfWork.Payments.FindAsync(p =>
                p.GatewayTransactionId == paymentIntent.Id);

            var payment = payments.FirstOrDefault();
            if (payment != null)
            {
                payment.Status = PaymentStatus.Paid;
                payment.ProcessedAt = DateTime.UtcNow;
                payment.GatewayResponse = "Payment succeeded via PaymentIntent";

                await _unitOfWork.Payments.UpdateAsync(payment);
                await _unitOfWork.Orders.UpdatePaymentStatusAsync(payment.OrderId, PaymentStatus.Paid);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task HandlePaymentIntentPaymentFailed(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null) return;

            // Find payment by payment intent ID
            var payments = await _unitOfWork.Payments.FindAsync(p =>
                p.GatewayTransactionId == paymentIntent.Id);

            var payment = payments.FirstOrDefault();
            if (payment != null)
            {
                payment.Status = PaymentStatus.Failed;
                payment.FailedAt = DateTime.UtcNow;
                payment.GatewayResponse = paymentIntent.LastPaymentError?.Message ?? "Payment failed";

                await _unitOfWork.Payments.UpdateAsync(payment);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private async Task ProcessStripePayment(string sessionId, PaymentStatus status)
        {
            var payments = await _unitOfWork.Payments.FindAsync(p =>
                p.GatewayTransactionId == sessionId && p.Status == PaymentStatus.Pending);

            var payment = payments.FirstOrDefault();
            if (payment == null) return;

            // Get session details from Stripe
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            payment.Status = status;

            if (status == PaymentStatus.Paid)
            {
                payment.ProcessedAt = DateTime.UtcNow;
                payment.GatewayResponse = "Payment completed successfully";

                // Update order
                await _unitOfWork.Orders.UpdatePaymentStatusAsync(payment.OrderId, PaymentStatus.Paid);

                var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId);
                if (order != null && order.Status == OrderStatus.Pending)
                {
                    await _orderService.UpdateOrderStatusAsync(order.Id, OrderStatus.Processing);
                }
            }
            else
            {
                payment.FailedAt = DateTime.UtcNow;
                payment.GatewayResponse = session.PaymentIntent?.LastPaymentError?.Message ?? "Payment failed";
            }

            await _unitOfWork.Payments.UpdateAsync(payment);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<PaymentDto> VerifyAndCompletePaymentAsync(string paymentReference)
        {
            var payment = await _unitOfWork.Payments.GetByReferenceAsync(paymentReference);
            if (payment == null)
            {
                throw new NotFoundException("Payment", paymentReference);
            }

            if (payment.Method == PaymentMethod.Stripe && payment.GatewayTransactionId != null)
            {
                // Verify with Stripe
                var service = new SessionService();
                var session = await service.GetAsync(payment.GatewayTransactionId);

                if (session.PaymentStatus == "paid" && payment.Status != PaymentStatus.Paid)
                {
                    payment.Status = PaymentStatus.Paid;
                    payment.ProcessedAt = DateTime.UtcNow;
                    payment.GatewayResponse = "Payment verified via Stripe";

                    await _unitOfWork.Payments.UpdateAsync(payment);
                    await _unitOfWork.Orders.UpdatePaymentStatusAsync(payment.OrderId, PaymentStatus.Paid);
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            return MapPaymentToDto(payment);
        }

        private string GeneratePaymentReference()
        {
            return $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
        }

        private PaymentDto MapPaymentToDto(Payment payment)
        {
            return new PaymentDto
            {
                Id = payment.Id,
                PaymentReference = payment.PaymentReference,
                OrderId = payment.OrderId,
                OrderNumber = payment.Order?.OrderNumber ?? string.Empty,
                Method = payment.Method.ToString(),
                Type = payment.Type.ToString(),
                Status = payment.Status.ToString(),
                Amount = payment.Amount,
                Currency = payment.Currency,
                GatewayTransactionId = payment.GatewayTransactionId,
                BankName = payment.BankName,
                AccountNumber = payment.AccountNumber,
                TransactionId = payment.TransactionId,
                DepositSlipUrl = payment.DepositSlipUrl,
                ApprovedBy = payment.ApprovedBy,
                ApprovedAt = payment.ApprovedAt,
                CreatedAt = payment.CreatedAt,
                ProcessedAt = payment.ProcessedAt,
                FailedAt = payment.FailedAt
            };
        }
    }

    public class PaymentSessionDto
    {
        public string SessionId { get; set; } = string.Empty;
        public Guid PaymentId { get; set; }
        public string PaymentReference { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
    }
}
