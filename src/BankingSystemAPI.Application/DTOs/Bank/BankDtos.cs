#region Usings
using BankingSystemAPI.Application.DTOs.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BankingSystemAPI.Domain.Constant;
#endregion


namespace BankingSystemAPI.Application.DTOs.Bank
{
    public class BankResDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<UserResDto> Users { get; set; } = new();
    }

    public class BankSimpleResDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class BankReqDto
    {
        [Required(ErrorMessage = ApiResponseMessages.Validation.BankNameRequired)]
        [StringLength(100, ErrorMessage = ApiResponseMessages.Validation.BankNameTooLong)]
        public string Name { get; set; } = string.Empty;
    }

    public class BankEditDto
    {
        [Required(ErrorMessage = ApiResponseMessages.Validation.BankNameRequired)]
        [StringLength(100, ErrorMessage = ApiResponseMessages.Validation.BankNameTooLong)]
        public string Name { get; set; } = string.Empty;
    }
}

