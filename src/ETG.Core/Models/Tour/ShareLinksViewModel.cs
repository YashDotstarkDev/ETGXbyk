using System;
using System.Collections.Generic;
using System.Text;

namespace ETG.Core.Models.Tour
{
    public sealed class ShareLinksViewModel
    {
        public string CurrentUrl { get; init; } = string.Empty;
        public string FacebookShareUrl { get; init; } = string.Empty;
        public string TwitterShareUrl { get; init; } = string.Empty;
        public string LinkedInShareUrl { get; init; } = string.Empty;
        public string PinterestShareUrl { get; init; } = string.Empty;
        public string CopyUrl { get; init; } = string.Empty;
    }
}
