namespace Codex.ObjectModel;

/// <summary>
/// WARNING: These values are used during serialization. The values should not be changed.
/// When adding new search types they should be added at the end. Removing a type should preserve 
/// integral values of other values.
/// </summary>
public enum SearchTypeId : byte
{
    Definition = 0,
    Reference = 1,
    TextChunk = 2,
    TextSource = 3,
    BoundSource = 4,
    Language = 5,
    Repository = 6,
    Project = 7,
    Commit = 8,
    CommitFiles = 9,
    ProjectReference = 10,
    Property = 11,
    StoredFilter = 12,
    RegisteredEntity = 13,
}