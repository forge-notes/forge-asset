namespace ForgeAsset.Api.Models;

public sealed class Asset
{
    public long Id { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Location { get; set; }
    public string? Owner { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public string? Remark { get; set; }
    public string Status { get; set; } = "normal";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
