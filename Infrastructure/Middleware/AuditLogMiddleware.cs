using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Middleware
{
    public class AuditLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuditLogMiddleware> _logger;

        public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            // Only log POST, PUT, DELETE requests
            if (context.Request.Method == HttpMethods.Post ||
                context.Request.Method == HttpMethods.Put ||
                context.Request.Method == HttpMethods.Delete)
            {
                // Store the original response body stream
                var originalBodyStream = context.Response.Body;

                using (var responseBody = new MemoryStream())
                {
                    context.Response.Body = responseBody;

                    // Read the request body
                    string requestBody = await GetRequestBodyAsync(context.Request);

                    await _next(context);

                    // Read the response body
                    string responseBodyContent = await GetResponseBodyAsync(context.Response);

                    // Log the audit trail
                    await LogAuditTrailAsync(context, requestBody, responseBodyContent, serviceProvider);

                    // Copy the response back to the original stream
                    await responseBody.CopyToAsync(originalBodyStream);
                }
            }
            else
            {
                await _next(context);
            }
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest request)
        {
            request.EnableBuffering();
            var body = request.Body;
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];
            await request.Body.ReadAsync(buffer, 0, buffer.Length);
            var bodyAsText = Encoding.UTF8.GetString(buffer);
            request.Body.Seek(0, SeekOrigin.Begin);
            return bodyAsText;
        }

        private async Task<string> GetResponseBodyAsync(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var text = await new StreamReader(response.Body).ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);
            return text;
        }

        private async Task LogAuditTrailAsync(HttpContext context, string requestBody, string responseBody, IServiceProvider serviceProvider)
        {
            try
            {
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;

                    // Parse userId as Guid or use empty Guid for anonymous
                    Guid userId;
                    if (!Guid.TryParse(userIdClaim, out userId))
                    {
                        userId = Guid.Empty;
                    }

                    var auditLog = new AuditLog
                    {
                        TableName = GetEntityNameFromPath(context.Request.Path),
                        RecordId = GetEntityIdFromPath(context.Request.Path) ?? Guid.Empty,
                        Action = $"{context.Request.Method} {context.Request.Path}",
                        UserId = userId,
                        UserEmail = userEmail ?? "anonymous@example.com",
                        OldValues = "", // You would need to get old values from database
                        NewValues = requestBody,
                        Timestamp = DateTime.UtcNow,
                        IpAddress = context.Connection.RemoteIpAddress?.ToString() ?? "",
                        UserAgent = context.Request.Headers["User-Agent"].ToString()
                    };

                    // Use the repository or DbContext directly
                    await dbContext.AuditLogs.AddAsync(auditLog);
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // Don't throw exceptions from middleware, just log
                _logger.LogError(ex, "Error logging audit trail");
            }
        }

        private string GetEntityNameFromPath(string path)
        {
            var segments = path.Split('/');
            if (segments.Length >= 2)
            {
                return segments[1];
            }
            return "Unknown";
        }

        private Guid? GetEntityIdFromPath(string path)
        {
            var segments = path.Split('/');
            if (segments.Length >= 3 && Guid.TryParse(segments[2], out var id))
            {
                return id;
            }
            return null;
        }
    }
}
