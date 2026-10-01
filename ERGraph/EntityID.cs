namespace ERGraph
{
    public struct EntityID : IEquatable<EntityID>
    {
        public readonly int Value;

        public EntityID(int value)
        {
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null || obj is not EntityID) return false;
            return Value == ((EntityID)obj).Value;
        }

        public bool Equals(EntityID other)
        {
            return Value == other.Value;
        }

        public override int GetHashCode()
        {
            return Value;
        }
    }

} // namespace ERGraph
