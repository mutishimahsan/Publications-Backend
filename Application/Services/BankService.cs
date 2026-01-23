using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services
{
    public interface IBankService
    {
        Task<IEnumerable<BankAccountDto>> GetBankAccountsAsync();
        Task<BankAccountDto> GetPrimaryBankAccountAsync();
    }

    public class BankService : IBankService
    {
        public Task<IEnumerable<BankAccountDto>> GetBankAccountsAsync()
        {
            // In production, this would come from database
            var bankAccounts = new List<BankAccountDto>
            {
                new BankAccountDto
                {
                    BankName = "Habib Bank Limited",
                    AccountTitle = "MENTISERA (SMC-Private) Limited",
                    AccountNumber = "0123456789012",
                    IBAN = "PK36HABB0000123456789012",
                    BranchCode = "0123",
                    BranchName = "Main Branch, Karachi"
                },
                new BankAccountDto
                {
                    BankName = "United Bank Limited",
                    AccountTitle = "MENTISERA Publications",
                    AccountNumber = "9876543210987",
                    IBAN = "PK03UNIL0109001234567890",
                    BranchCode = "0109",
                    BranchName = "Gulshan Branch, Lahore"
                }
            };

            return Task.FromResult<IEnumerable<BankAccountDto>>(bankAccounts);
        }

        public Task<BankAccountDto> GetPrimaryBankAccountAsync()
        {
            var primaryAccount = new BankAccountDto
            {
                BankName = "Habib Bank Limited",
                AccountTitle = "MENTISERA (SMC-Private) Limited",
                AccountNumber = "0123456789012",
                IBAN = "PK36HABB0000123456789012",
                BranchCode = "0123",
                BranchName = "Main Branch, Karachi"
            };

            return Task.FromResult(primaryAccount);
        }
    }
}