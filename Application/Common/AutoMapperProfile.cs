using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Common
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            // User mappings
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.UserType, opt => opt.MapFrom(src => src.UserType.ToString()));

            // Product mappings
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Format.ToString()))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.Authors, opt => opt.MapFrom(src => src.ProductAuthors.Select(pa => pa.Author)));

            // Category mappings
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.ParentCategoryName, opt => opt.MapFrom(src => src.ParentCategory != null ? src.ParentCategory.Name : null))
                .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count(p => !p.IsDeleted)));

            // Author mappings
            CreateMap<Author, AuthorDto>();

            // Order mappings
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
                .ForMember(dest => dest.FulfillmentStatus, opt => opt.MapFrom(src => src.FulfillmentStatus.ToString()));

            // OrderItem mappings
            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(dest => dest.ProductTitle, opt => opt.MapFrom(src => src.Product.Title))
                .ForMember(dest => dest.CoverImageUrl, opt => opt.MapFrom(src => src.Product.CoverImageUrl))
                .ForMember(dest => dest.Format, opt => opt.MapFrom(src => src.Product.Format.ToString()));

            // Payment mappings
            CreateMap<Payment, PaymentDto>()
                .ForMember(dest => dest.Method, opt => opt.MapFrom(src => src.Method.ToString()))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.Order.OrderNumber));

            // Invoice mappings
            CreateMap<Invoice, InvoiceDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.Order.OrderNumber));

            // Address mappings
            CreateMap<Address, AddressDto>();
            CreateMap<CreateAddressDto, Address>();

            #region Blog Mappings

            // Blog mappings - FIXED: Remove ReverseMap and add specific mappings
            CreateMap<Blog, BlogDto>()
                .ForMember(dest => dest.Categories, opt =>
                    opt.MapFrom(src => src.BlogCategories))
                .ForMember(dest => dest.Tags, opt =>
                    opt.MapFrom(src => src.BlogTags))
                .ForMember(dest => dest.IsPublished, opt =>
                    opt.MapFrom(src => src.IsPublished && src.Status == BlogStatus.Published))
                .ForMember(dest => dest.Status, opt =>
                    opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.AuthorName, opt =>
                    opt.MapFrom(src => src.Author != null ? src.Author.FullName : null))
                .ForMember(dest => dest.CommentCount, opt =>
                    opt.MapFrom(src => src.Comments.Count(c => !c.IsDeleted && c.Status == CommentStatus.Approved)));

            CreateMap<CreateBlogDto, Blog>()
                .ForMember(dest => dest.BlogCategories, opt => opt.Ignore())
                .ForMember(dest => dest.BlogTags, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.ViewCount, opt => opt.Ignore())
                .ForMember(dest => dest.PublishedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
                .ForMember(dest => dest.Author, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore());

            CreateMap<UpdateBlogDto, Blog>()
                .ForMember(dest => dest.BlogCategories, opt => opt.Ignore())
                .ForMember(dest => dest.BlogTags, opt => opt.Ignore())
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.ViewCount, opt => opt.Ignore())
                .ForMember(dest => dest.PublishedDate, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.AuthorId, opt => opt.Ignore())
                .ForMember(dest => dest.Author, opt => opt.Ignore())
                .ForMember(dest => dest.Comments, opt => opt.Ignore());

            // BlogCategory mappings
            CreateMap<BlogCategory, BlogCategoryDto>()
                .ForMember(dest => dest.BlogCount, opt =>
                    opt.MapFrom(src => src.Blogs.Count(b => !b.IsDeleted && b.IsPublished && b.Status == BlogStatus.Published)))
                .ReverseMap();

            CreateMap<CreateBlogCategoryDto, BlogCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.Blogs, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            CreateMap<UpdateBlogCategoryDto, BlogCategory>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.Blogs, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // BlogTag mappings - FIXED: Add BlogCount property mapping
            CreateMap<BlogTag, BlogTagDto>()
                .ForMember(dest => dest.BlogCount, opt =>
                    opt.MapFrom(src => src.Blogs.Count(b => !b.IsDeleted && b.IsPublished && b.Status == BlogStatus.Published)))
                .ReverseMap();

            CreateMap<CreateBlogTagDto, BlogTag>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.Blogs, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            CreateMap<UpdateBlogTagDto, BlogTag>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.Blogs, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

            // BlogComment mappings
            CreateMap<BlogComment, BlogCommentDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ReverseMap();

            CreateMap<CreateBlogCommentDto, BlogComment>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.Blog, opt => opt.Ignore())
                .ForMember(dest => dest.ParentComment, opt => opt.Ignore())
                .ForMember(dest => dest.Replies, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.IsApproved, opt => opt.Ignore());

            #endregion
        }
    }
}
