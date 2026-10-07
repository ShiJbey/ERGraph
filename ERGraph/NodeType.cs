namespace ERGraph
{
    /// <summary>
    /// Enum value used to distinguish nodes with multiple traits (entity nodes) from
    /// entities with single traits (value node).
    /// </summary>
    public enum NodeType : int
    {
        Entity = 0,
        Value = 1,
        Relationship = 2
    }
}