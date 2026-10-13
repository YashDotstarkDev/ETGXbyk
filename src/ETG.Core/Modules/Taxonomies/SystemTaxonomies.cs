namespace ETG.Core.Modules;

/// <summary>
/// Taxonomies required by the system
/// </summary>
public static partial class SystemTaxonomies
{
    public static EmbedTypeTaxonomy EmbedType { get; } = new();

    public static readonly IReadOnlyList<ISystemTaxonomy> ProtectedTaxonomies =
    [
        EmbedType
    ];

    public static bool Includes(TaxonomyInfo taxonomy) =>
        ProtectedTaxonomies.Select(t => t.TaxonomyGUID).Contains(taxonomy.TaxonomyGUID);

    public static bool Includes(TagInfo tag) =>
        ProtectedTaxonomies.SelectMany(t => t.ProtectedTags.Select(t => t.TagGUID)).Contains(tag.TagGUID);
}
