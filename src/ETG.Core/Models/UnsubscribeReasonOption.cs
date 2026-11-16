using System;

namespace ETG.Core.Models;

public sealed record UnsubscribeReasonOption(
    Guid ItemGuid,
    string Text);