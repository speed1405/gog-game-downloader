using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GogGameDownloader.Models;

public class Game
{
    [Key]
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int? ReleaseYear { get; set; }
    public string? GenreJson { get; set; }
    public string? PosterUrl { get; set; }
    public string? BackgroundUrl { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<GameVersion> Versions { get; set; } = new List<GameVersion>();
}
