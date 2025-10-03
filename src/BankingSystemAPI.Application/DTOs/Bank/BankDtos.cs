using BankingSystemAPI.Application.DTOs.User;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BankingSystemAPI.Application.DTOs.Bank
{
    public class BankResDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public List<UserResDto> Users { get; set; } = new();
    }

    public class BankSimpleResDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class BankReqDto
    {
        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
    }

    public class BankEditDto
    {
        [Required(ErrorMessage = "Bank name is required")]
        [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
        public string Name { get; set; } = string.Empty;
    }
}
