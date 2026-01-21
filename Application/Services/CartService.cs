using Application.DTOs;
using Application.Interfaces;
using Domain.Common;
using Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(Guid? userId = null, string sessionId = null);
        Task<CartDto> AddToCartAsync(AddToCartDto dto, Guid? userId = null, string sessionId = null);
        Task<CartDto> UpdateCartItemAsync(Guid productId, UpdateCartItemDto dto, Guid? userId = null, string sessionId = null);
        Task<CartDto> RemoveFromCartAsync(Guid productId, Guid? userId = null, string sessionId = null);
        Task ClearCartAsync(Guid? userId = null, string sessionId = null);
        Task<int> GetCartItemCountAsync(Guid? userId = null, string sessionId = null);
        Task<CartDto> MergeCartsAsync(Guid userId, string sessionId);
    }

    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IProductService _productService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDistributedCache _cache;

        public CartService(
            IUnitOfWork unitOfWork,
            IProductService productService,
            IHttpContextAccessor httpContextAccessor,
            IDistributedCache cache)
        {
            _unitOfWork = unitOfWork;
            _productService = productService;
            _httpContextAccessor = httpContextAccessor;
            _cache = cache;
        }

        private string GetCartKey(Guid? userId, string sessionId)
        {
            if (userId.HasValue)
                return $"cart_user_{userId}";

            if (!string.IsNullOrEmpty(sessionId))
                return $"cart_session_{sessionId}";

            throw new ArgumentException("Either userId or sessionId must be provided");
        }

        private async Task<Cart> GetOrCreateCartAsync(Guid? userId = null, string sessionId = null)
        {
            var cartKey = GetCartKey(userId, sessionId);
            var cartJson = await _cache.GetStringAsync(cartKey);

            if (!string.IsNullOrEmpty(cartJson))
            {
                return JsonConvert.DeserializeObject<Cart>(cartJson);
            }

            return new Cart
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                SessionId = sessionId,
                Items = new List<CartItem>(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private async Task SaveCartAsync(Cart cart)
        {
            var cartKey = GetCartKey(cart.UserId, cart.SessionId);
            var cartJson = JsonConvert.SerializeObject(cart);

            await _cache.SetStringAsync(cartKey, cartJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30) // Cart expires after 30 days
            });

            cart.UpdatedAt = DateTime.UtcNow;
        }

        public async Task<CartDto> GetCartAsync(Guid? userId = null, string sessionId = null)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            return await MapCartToDtoAsync(cart);
        }

        public async Task<CartDto> AddToCartAsync(AddToCartDto dto, Guid? userId = null, string sessionId = null)
        {
            // Validate product
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null || product.IsDeleted || product.Status != ProductStatus.Published)
            {
                throw new NotFoundException("Product", dto.ProductId);
            }

            // Check availability
            var isAvailable = await _productService.IsProductAvailableAsync(dto.ProductId, dto.Quantity);
            if (!isAvailable)
            {
                throw new ValidationException($"Product is not available in the requested quantity. Available: {product.StockQuantity}");
            }

            var cart = await GetOrCreateCartAsync(userId, sessionId);

            // Check if product already in cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);

            if (existingItem != null)
            {
                // Update quantity
                existingItem.Quantity += dto.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Add new item
                cart.Items.Add(new CartItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.DiscountPrice ?? product.Price,
                    AddedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await SaveCartAsync(cart);
            return await MapCartToDtoAsync(cart);
        }

        public async Task<CartDto> UpdateCartItemAsync(Guid productId, UpdateCartItemDto dto, Guid? userId = null, string sessionId = null)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem == null)
            {
                throw new NotFoundException("Cart item", productId);
            }

            if (dto.Quantity <= 0)
            {
                cart.Items.Remove(cartItem);
            }
            else
            {
                // Check availability
                var isAvailable = await _productService.IsProductAvailableAsync(productId, dto.Quantity);
                if (!isAvailable)
                {
                    throw new ValidationException($"Product is not available in the requested quantity.");
                }

                cartItem.Quantity = dto.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
            }

            await SaveCartAsync(cart);
            return await MapCartToDtoAsync(cart);
        }

        public async Task<CartDto> RemoveFromCartAsync(Guid productId, Guid? userId = null, string sessionId = null)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            var cartItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);

            if (cartItem == null)
            {
                throw new NotFoundException("Cart item", productId);
            }

            cart.Items.Remove(cartItem);
            await SaveCartAsync(cart);

            return await MapCartToDtoAsync(cart);
        }

        public async Task ClearCartAsync(Guid? userId = null, string sessionId = null)
        {
            var cartKey = GetCartKey(userId, sessionId);
            await _cache.RemoveAsync(cartKey);
        }

        public async Task<int> GetCartItemCountAsync(Guid? userId = null, string sessionId = null)
        {
            var cart = await GetOrCreateCartAsync(userId, sessionId);
            return cart.Items.Sum(i => i.Quantity);
        }

        public async Task<CartDto> MergeCartsAsync(Guid userId, string sessionId)
        {
            // Get session cart
            var sessionCart = await GetOrCreateCartAsync(null, sessionId);

            // Get user cart
            var userCart = await GetOrCreateCartAsync(userId, null);

            // Merge items from session cart to user cart
            foreach (var sessionItem in sessionCart.Items)
            {
                var existingItem = userCart.Items.FirstOrDefault(i => i.ProductId == sessionItem.ProductId);

                if (existingItem != null)
                {
                    existingItem.Quantity += sessionItem.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    userCart.Items.Add(new CartItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = sessionItem.ProductId,
                        Quantity = sessionItem.Quantity,
                        UnitPrice = sessionItem.UnitPrice,
                        AddedAt = sessionItem.AddedAt,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            // Save merged cart
            userCart.UserId = userId;
            await SaveCartAsync(userCart);

            // Clear session cart
            await ClearCartAsync(null, sessionId);

            return await MapCartToDtoAsync(userCart);
        }

        private async Task<CartDto> MapCartToDtoAsync(Cart cart)
        {
            var cartDto = new CartDto
            {
                CartId = cart.Id,
                UserId = cart.UserId,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt
            };

            foreach (var item in cart.Items)
            {
                var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (product != null && !product.IsDeleted && product.Status == ProductStatus.Published)
                {
                    var cartItemDto = new CartItemDto
                    {
                        ProductId = item.ProductId,
                        ProductTitle = product.Title,
                        CoverImageUrl = product.CoverImageUrl,
                        Format = product.Format.ToString(),
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        DiscountPrice = product.DiscountPrice
                    };

                    cartDto.Items.Add(cartItemDto);
                }
            }

            return cartDto;
        }
    }

    public class Cart
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public string? SessionId { get; set; }
        public List<CartItem> Items { get; set; } = new List<CartItem>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CartItem
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
