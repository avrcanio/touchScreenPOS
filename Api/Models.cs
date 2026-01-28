using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TouchScreenPOS.Api;

public sealed class Representation
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("occurred_at")]
    public DateTimeOffset OccurredAt { get; set; }

    [JsonPropertyName("warehouse")]
    public int Warehouse { get; set; }

    [JsonPropertyName("user")]
    public int User { get; set; }

    [JsonPropertyName("reason_id")]
    public int ReasonId { get; set; }

    [JsonPropertyName("reason_name")]
    public string? ReasonName { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("items")]
    public List<RepresentationItem> Items { get; set; } = new();
}

public sealed class RepresentationItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("artikl")]
    public int Artikl { get; set; }

    public string? ArtiklName { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}

public sealed class RepresentationReason
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }
}

public sealed class DrinkCategory
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("parent_id")]
    public int? ParentId { get; set; }

    [JsonPropertyName("parent_name")]
    public string? ParentName { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("sort_order")]
    public int SortOrder { get; set; }
}

public sealed class Artikl
{
    [JsonPropertyName("rm_id")]
    public int RmId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("image")]
    public string? Image { get; set; }

    [JsonPropertyName("image_46x75")]
    public string? Image46x75 { get; set; }

    [JsonPropertyName("image_125x200")]
    public string? Image125x200 { get; set; }

    [JsonPropertyName("drink_category_id")]
    public int? DrinkCategoryId { get; set; }

    [JsonPropertyName("drink_category_name")]
    public string? DrinkCategoryName { get; set; }

    [JsonPropertyName("is_sellable")]
    public bool IsSellable { get; set; }

    [JsonPropertyName("is_stock_item")]
    public bool IsStockItem { get; set; }
}

public sealed class RepresentationCreateRequest
{
    [JsonPropertyName("warehouse")]
    public int Warehouse { get; set; }

    [JsonPropertyName("reason_id")]
    public int ReasonId { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("items")]
    public List<RepresentationCreateItem> Items { get; set; } = new();
}

public sealed class RepresentationCreateItem
{
    [JsonPropertyName("artikl")]
    public int Artikl { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }
}

public sealed class ApiUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("is_staff")]
    public bool IsStaff { get; set; }

    [JsonPropertyName("is_superuser")]
    public bool IsSuperuser { get; set; }
}

public sealed class RepresentationView
{
    public int Id { get; init; }
    public string? OccurredAtDisplay { get; init; }
    public int UserId { get; init; }
    public string? UserName { get; init; }
    public string? ReasonName { get; init; }
    public string? Note { get; init; }
    public int ItemCount { get; init; }
    public Representation Source { get; init; } = null!;
}

public sealed class Warehouse
{
    [JsonPropertyName("rm_id")]
    public int RmId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }

    [JsonPropertyName("ordinal")]
    public decimal Ordinal { get; set; }
}
