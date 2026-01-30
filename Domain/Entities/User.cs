using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string? FullName { get; set; }
        public UserType UserType { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public bool IsDeleted { get; set; }
        public string UpdatedBy { get; set; } = "SYSTEM";
        public string CreatedBy { get; set; } = "SYSTEM";
        //public DateTime? LastLoginAt { get; set; }
    }
}
