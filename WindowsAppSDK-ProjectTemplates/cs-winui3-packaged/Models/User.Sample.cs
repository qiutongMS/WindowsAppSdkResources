using System;

namespace BlankApp.Models;

/// <summary>
/// Sample user model kept minimal for demo scenarios.
/// </summary>
public class UserSample
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
