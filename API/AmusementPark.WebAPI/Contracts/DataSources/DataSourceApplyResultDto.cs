using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AmusementPark.WebAPI.Contracts.DataSources;

public sealed class DataSourceApplyResultDto
{
    public int AppliedCount { get; set; }
}
