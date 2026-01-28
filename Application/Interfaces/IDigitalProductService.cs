using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IDigitalProductService
    {
        // Digital file management
        Task<string> UploadDigitalFileAsync(UploadDigitalProductDto dto);
        Task<bool> DeleteDigitalFileAsync(Guid productId);
        Task<DigitalDownloadDto> GenerateDownloadLinkAsync(Guid orderItemId, Guid customerId);

        // Digital access management
        Task<DigitalAccessDto> GetDigitalAccessAsync(Guid digitalAccessId);
        Task<DigitalAccessDto> GetDigitalAccessByOrderItemAsync(Guid orderItemId);
        Task<IEnumerable<DigitalAccessDto>> GetCustomerDigitalAccessAsync(Guid customerId);
        Task<DigitalAccessDto> GrantDigitalAccessAsync(Guid orderItemId);
        Task<DigitalAccessDto> UpdateDigitalAccessAsync(Guid digitalAccessId, UpdateDigitalAccessDto dto);
        Task<bool> RevokeDigitalAccessAsync(Guid digitalAccessId);

        // Download processing
        Task<FileDownloadResult> ProcessDownloadAsync(string token);
        Task<bool> ValidateDownloadTokenAsync(string token);

        // Administrative functions
        Task<IEnumerable<DigitalAccessDto>> GetExpiredAccessAsync();
        Task<bool> CleanupExpiredAccessAsync();
        Task<DigitalAccessStatsDto> GetDigitalAccessStatsAsync();
    }
}
