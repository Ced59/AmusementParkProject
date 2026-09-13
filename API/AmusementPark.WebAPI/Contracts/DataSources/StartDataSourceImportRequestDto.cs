using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class StartDataSourceImportRequestDto
{
    [Required]
    public string ImportKind { get; set; } = "sitemap";

    public List<string> Urls { get; set; } = new List<string>();

    public Dictionary<string, string?> Options { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    public string? ResumeSessionId { get; set; }
}
