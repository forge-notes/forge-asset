using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ForgeAsset.Api.Contracts;
using ForgeAsset.Api.Data;
using ForgeAsset.Api.Models;
using ForgeAsset.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration["Database:ConnectionString"] ?? "Data Source=forge_asset.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (databaseProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        options.UseMySQL(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173", "http://localhost:8080")
        .AllowAnyHeader()
        .AllowAnyMethod()));
builder.Services.AddSingleton<AssetCodeGenerator>();
var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/auth/login", (LoginRequest request, IConfiguration configuration) =>
{
    var username = configuration["AdminUser:Username"] ?? "admin";
    var password = configuration["AdminUser:Password"] ?? "admin123";
    if (!string.Equals(request.Username, username, StringComparison.Ordinal) || request.Password != password)
    {
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, username),
        new Claim(ClaimTypes.Name, username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };
    var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: configuration["Jwt:Issuer"],
        audience: configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(12),
        signingCredentials: credentials);

    return Results.Ok(new
    {
        token = new JwtSecurityTokenHandler().WriteToken(token),
        username
    });
}).AllowAnonymous();

var assets = app.MapGroup("/api/assets").RequireAuthorization();

assets.MapGet("/", async (string? keyword, AppDbContext db, CancellationToken ct) =>
{
    var query = db.Assets.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(keyword))
    {
        var term = keyword.Trim();
        query = query.Where(x => x.AssetCode.Contains(term) || x.Name.Contains(term));
    }
    return Results.Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync(ct));
});

assets.MapGet("/{id:long}", async (long id, AppDbContext db, CancellationToken ct) =>
{
    var asset = await db.Assets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    return asset is null ? Results.NotFound() : Results.Ok(asset);
});

assets.MapPost("/generate-code", async (AssetCodeGenerator generator, AppDbContext db, CancellationToken ct) =>
    Results.Ok(new { assetCode = await generator.GenerateAsync(db, ct) }));

assets.MapPost("/", async (AssetRequest request, AssetCodeGenerator generator, AppDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["资产名称不能为空"] });
    }

    var code = string.IsNullOrWhiteSpace(request.AssetCode)
        ? await generator.GenerateAsync(db, ct)
        : request.AssetCode.Trim();
    if (await db.Assets.IgnoreQueryFilters().AnyAsync(x => x.AssetCode == code, ct))
    {
        return Results.Conflict(new { message = "资产编号已存在" });
    }

    var asset = new Asset();
    Apply(request, asset, code);
    db.Assets.Add(asset);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/assets/{asset.Id}", asset);
});

assets.MapPut("/{id:long}", async (long id, AssetRequest request, AppDbContext db, CancellationToken ct) =>
{
    var asset = await db.Assets.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (asset is null) return Results.NotFound();
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["资产名称不能为空"] });
    }

    var code = string.IsNullOrWhiteSpace(request.AssetCode) ? asset.AssetCode : request.AssetCode.Trim();
    if (await db.Assets.IgnoreQueryFilters().AnyAsync(x => x.AssetCode == code && x.Id != id, ct))
    {
        return Results.Conflict(new { message = "资产编号已存在" });
    }

    Apply(request, asset, code);
    await db.SaveChangesAsync(ct);
    return Results.Ok(asset);
});

assets.MapDelete("/{id:long}", async (long id, AppDbContext db, CancellationToken ct) =>
{
    var asset = await db.Assets.FirstOrDefaultAsync(x => x.Id == id, ct);
    if (asset is null) return Results.NotFound();
    asset.DeletedAt = DateTime.UtcNow;
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

app.Run();

static void Apply(AssetRequest request, Asset asset, string code)
{
    asset.AssetCode = code;
    asset.Name = request.Name.Trim();
    asset.Category = Normalize(request.Category);
    asset.Brand = Normalize(request.Brand);
    asset.Model = Normalize(request.Model);
    asset.SerialNumber = Normalize(request.SerialNumber);
    asset.Location = Normalize(request.Location);
    asset.Owner = Normalize(request.Owner);
    asset.PurchaseDate = request.PurchaseDate;
    asset.Remark = Normalize(request.Remark);
    asset.Status = string.IsNullOrWhiteSpace(request.Status) ? "normal" : request.Status.Trim();
}

static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

public partial class Program;
