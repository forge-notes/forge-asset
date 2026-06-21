namespace ForgeAsset.Api.Contracts;

public sealed record LoginRequest(string Username, string Password);

public sealed record AssetRequest(
    string? AssetCode,
    string Name,
    string? Category,
    string? Brand,
    string? Model,
    string? SerialNumber,
    string? Location,
    string? Owner,
    DateTime? PurchaseDate,
    string? Remark,
    string? Status);
