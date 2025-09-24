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
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class BankEditDto
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; }
    }
}
