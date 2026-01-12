using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Common
{
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }

    public class NotFoundException : DomainException
    {
        public NotFoundException(string entity, Guid id)
            : base($"{entity} with ID {id} was not found.") { }

        public NotFoundException(string entity, string identifier)
            : base($"{entity} with identifier {identifier} was not found.") { }
    }

    public class ValidationException : DomainException
    {
        public ValidationException(string message) : base(message) { }
    }

    public class PaymentException : DomainException
    {
        public PaymentException(string message) : base(message) { }
    }

    public class DownloadException : DomainException
    {
        public DownloadException(string message) : base(message) { }
    }
}
