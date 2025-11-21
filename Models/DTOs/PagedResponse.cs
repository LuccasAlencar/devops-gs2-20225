namespace dotnet_gs2_2025.Models.DTOs;

public class PagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public List<T> Data { get; set; } = new();
    public List<Link> Links { get; set; } = new();
}
