using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace dotnet_gs2_2025.Models.DTOs;

public class ResumeUploadRequestDto
{
    [Required]
    public IFormFile? File { get; set; }
}

