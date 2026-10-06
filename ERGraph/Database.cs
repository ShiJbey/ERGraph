using ERGraph.Errors;
using System;
using System.Collections.Generic;

namespace ERGraph
{
    using EntityID = int;

    public class Database
    {
        private static readonly int s_columnSizeIncrement = 64;

        /// <summary>
        /// The ID automatically assigned to the next entity assuming
        /// one was not provided or free from a previous entity.
        /// </summary>
        private int m_nextEntityId = 0;
        /// <summary>
        /// Entity IDs available to assign to an entity or relationship.
        /// </summary>
        private readonly Queue<EntityID> m_freeEntityIds = new();
        /// <summary>
        /// IDs of active entity nodes.
        /// </summary>
        private readonly HashSet<EntityID> m_activeEntityIds = new();
        /// <summary>
        /// A map of name aliases entity IDs.
        /// </summary>
        private readonly Dictionary<string, EntityID> m_nameToEntityMap = new();
        /// <summary>
        /// A map of entity IDs to their name aliases.
        /// </summary>
        private readonly Dictionary<EntityID, string> m_entityToNameMap = new();
        /// <summary>
        /// IDs of active relationship edges.
        /// </summary>
        private readonly HashSet<EntityID> m_activeRelationshipIds = new();
        /// <summary>
        /// Data columns containing trait data for entities and relationships
        /// </summary>
        private readonly Dictionary<string, IColumn> m_columns = new();
        /// <summary>
        /// Entities mapped to list of their outgoing edges.
        /// </summary>
        private readonly Dictionary<EntityID, List<EntityID>> m_outgoingEdges = new();
        /// <summary>
        /// Entities mapped to a list of their incoming edges.
        /// </summary>
        private readonly Dictionary<EntityID, List<EntityID>> m_incomingEdges = new();
        /// <summary>
        /// Array indices available for storing the source and target IDs of new
        /// relationships.
        /// </summary>
        private readonly Queue<int> m_freeSourceTargetIndices = new();
        /// <summary>
        /// An array containing the IDs of relationship source entities.
        /// </summary>
        private EntityID[] m_sources = new EntityID[s_columnSizeIncrement];
        /// <summary>
        /// An array containing the IDs of relationship target entities.
        /// </summary>
        private EntityID[] m_targets = new EntityID[s_columnSizeIncrement];
        /// <summary>
        /// The next available index assigned to source/target IDs when
        /// no free indices are available.
        /// </summary>
        private int m_nextSourceTargetIndex = 0;
        /// <summary>
        /// A map of relationship IDs to their associated index within the source
        /// and target arrays.
        /// </summary>
        private readonly Dictionary<EntityID, int> m_edgeToSourceTargetIndex = new();

        /// <summary>
        /// Create a new entity node in the database.
        /// </summary>
        /// <returns></returns>
        public EntityID CreateEntity()
        {
            EntityID entityId = m_freeEntityIds.Count > 0
                ? m_freeEntityIds.Dequeue()
                : m_nextEntityId++;
            m_activeEntityIds.Add(entityId);
            return entityId;
        }

        /// <summary>
        /// Create a new entity and assign the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public EntityID CreateEntity(string name)
        {
            if (m_nameToEntityMap.ContainsKey(name))
            {
                throw new DuplicateNameException(
                    $"{name} is already assigned to another entity");
            }

            EntityID entityId = CreateEntity();
            m_nameToEntityMap.Add(name, entityId);
            m_entityToNameMap.Add(entityId, name);
            return entityId;
        }

        /// <summary>
        /// Get the ID of an entity using its name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public EntityID GetEntityByName(string name)
        {
            return m_nameToEntityMap[name];
        }

        /// <summary>
        /// Attempt to retrieve an entity using it's name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public bool TryGetEntityByName(string name, out EntityID entityId)
        {
            return m_nameToEntityMap.TryGetValue(name, out entityId);
        }

        /// <summary>
        /// Assign the given name to the entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="name"></param>
        /// <exception cref="DuplicateNameException"></exception>
        public void SetEntityName(EntityID entityId, string name)
        {
            if (m_nameToEntityMap.ContainsKey(name))
            {
                throw new DuplicateNameException(
                    $"{name} is already assigned to another entity");
            }

            m_nameToEntityMap.Add(name, entityId);
            m_entityToNameMap.Add(entityId, name);
        }

        /// <summary>
        /// Check if an entity node exists in the database.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public bool EntityExists(EntityID entityId)
        {
            return m_activeEntityIds.Contains(entityId);
        }

        /// <summary>
        /// Destroy a node in the database along with all its associated
        /// relationship edges.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public bool DestroyEntity(EntityID entityId)
        {
            if (!EntityExists(entityId))
            {
                // Attempt to destroy the entity as a relationship.
                return DestroyRelationship(entityId);
            }

            // Remove the name if assigned.
            if (m_entityToNameMap.TryGetValue(entityId, out var name))
            {
                m_nameToEntityMap.Remove(name);
                m_entityToNameMap.Remove(entityId);
            }

            m_activeEntityIds.Remove(entityId);
            m_freeEntityIds.Enqueue(entityId);

            DeleteAllTraitsForEntity(entityId);
            DestroyAllRelationshipsForEntity(entityId);

            return true;
        }

        /// <summary>
        /// Create a new relationship edge between two entities.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="targetId"></param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException"></exception>
        public EntityID CreateRelationship(EntityID sourceId, EntityID targetId)
        {
            if (!EntityExists(sourceId))
            {
                throw new EntityNotFoundException($"No entity found with id: {sourceId}");
            }

            if (!EntityExists(targetId))
            {
                throw new EntityNotFoundException($"No entity found with id: {targetId}");
            }

            EntityID relationship = CreateEntity();
            m_activeRelationshipIds.Add(relationship);

            int sourceTargetIndex = GetFreeSourceTargetIndex();

            m_sources[sourceTargetIndex] = sourceId;
            m_targets[sourceTargetIndex] = targetId;
            m_edgeToSourceTargetIndex.Add(relationship, sourceTargetIndex);

            AddRelationshipDictEntry(m_outgoingEdges, sourceId, relationship);
            AddRelationshipDictEntry(m_incomingEdges, targetId, relationship);

            return relationship;
        }

        /// <summary>
        /// Destroy the relationship with the given id.
        /// </summary>
        /// <param name="relationshipId"></param>
        /// <returns></returns>
        public bool DestroyRelationship(EntityID relationshipId)
        {
            if (!m_activeRelationshipIds.Contains(relationshipId))
            {
                return false;
            }

            m_activeRelationshipIds.Remove(relationshipId);
            m_freeEntityIds.Enqueue(relationshipId);

            DeleteAllTraitsForEntity(relationshipId);

            RemoveRelationshipDictEntry(m_incomingEdges, relationshipId);
            RemoveRelationshipDictEntry(m_outgoingEdges, relationshipId);

            m_edgeToSourceTargetIndex.Remove(relationshipId);

            return true;
        }

        /// <summary>
        /// Destroy the relationship between the given entity nodes.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="targetId"></param>
        /// <returns></returns>
        public bool DestroyRelationship(EntityID sourceId, EntityID targetId)
        {
            if (TryGetRelationship(sourceId, targetId, out var relationshipId))
            {
                return DestroyRelationship(relationshipId);
            }

            return false;
        }

        /// <summary>
        /// Get the ID of the relationship between two entity nodes.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="targetId"></param>
        /// <returns></returns>
        /// <exception cref="RelationshipNotFoundException"></exception>
        public EntityID GetRelationship(EntityID sourceId, EntityID targetId)
        {
            if (TryGetRelationship(sourceId, targetId, out var relationshipId))
            {
                return relationshipId;
            }

            throw new RelationshipNotFoundException(
                $"No relationship found between {sourceId} and {targetId}");
        }

        /// <summary>
        /// Check if a relationship exists between two characters.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="targetId"></param>
        /// <returns></returns>
        public bool RelationshipExists(EntityID sourceId, EntityID targetId)
        {
            return TryGetRelationship(sourceId, targetId, out var _);
        }

        /// <summary>
        /// Check if a relationship exists between two characters.
        /// </summary>
        /// <param name="relationshipId"></param>
        /// <returns></returns>
        public bool RelationshipExists(EntityID relationshipId)
        {
            return m_activeRelationshipIds.Contains(relationshipId);
        }

        /// <summary>
        /// Attempt to get the ID of a relationship between two entity nodes.
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="targetId"></param>
        /// <param name="relationshipId"></param>
        /// <returns></returns>
        public bool TryGetRelationship(EntityID sourceId, EntityID targetId, out EntityID relationshipId)
        {
            if (m_outgoingEdges.TryGetValue(sourceId, out var relationshipIds))
            {
                foreach (var id in relationshipIds)
                {
                    int sourceTargetIndex = m_edgeToSourceTargetIndex[id];
                    if (m_targets[sourceTargetIndex] == targetId)
                    {
                        relationshipId = id;
                        return true;
                    }
                }
            }

            relationshipId = default;
            return false;
        }

        /// <summary>
        /// Get the source and target Ids of the given relationship
        /// </summary>
        /// <param name="relationshipId"></param>
        /// <returns></returns>
        public (EntityID, EntityID) GetEndpoints(EntityID relationshipId)
        {
            int sourceTargetIndex = m_edgeToSourceTargetIndex[relationshipId];
            return (m_sources[sourceTargetIndex], m_targets[sourceTargetIndex]);
        }

        /// <summary>
        /// Get outgoing edges from a given entity node.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public IEnumerable<EntityID> GetEdgesFrom(EntityID entityId)
        {
            if (m_outgoingEdges.TryGetValue(entityId, out var edges))
            {
                foreach (var edge in edges)
                {
                    yield return edge;
                }
            }
        }

        /// <summary>
        /// Get incoming edges for the given entity node.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public IEnumerable<EntityID> GetEdgesTo(EntityID entityId)
        {
            if (m_incomingEdges.TryGetValue(entityId, out var edges))
            {
                foreach (var edge in edges)
                {
                    yield return edge;
                }
            }
        }

        /// <summary>
        /// Set an integer trait's value.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <param name="value"></param>
        public void SetIntTrait(EntityID entityId, string trait, int value)
        {
            SetTrait(entityId, trait, value);
        }

        /// <summary>
        /// Set a float trait's value.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <param name="value"></param>
        public void SetFloatTrait(EntityID entityId, string trait, float value)
        {
            SetTrait(entityId, trait, value);
        }

        /// <summary>
        /// Set a string trait's value.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <param name="value"></param>
        public void SetStringTrait(EntityID entityId, string trait, string value)
        {
            SetTrait(entityId, trait, value);
        }

        /// <summary>
        /// Set a boolean trait's value.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <param name="value"></param>
        public void SetBoolTrait(EntityID entityId, string trait, bool value)
        {
            SetTrait(entityId, trait, value);
        }

        /// <summary>
        /// Sets a boolean flag trait to true.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        public void SetFlagTrait(EntityID entityId, string trait)
        {
            SetTrait(entityId, trait, true);
        }

        /// <summary>
        /// Removes a flag trait from an entity node.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        public void UnsetFlagTrait(EntityID entityId, string trait)
        {
            RemoveTrait(entityId, trait);
        }

        /// <summary>
        /// Get the int value of a trait.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <returns></returns>
        /// <exception cref="TraitNotFoundException"></exception>
        public int GetIntTrait(EntityID entityId, string trait)
        {
            if (TryGetTrait(entityId, trait, out int value))
            {
                return value;
            }

            throw new TraitNotFoundException($"Entity does not have trait: {trait}");
        }

        /// <summary>
        /// Get the float value of a trait.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <returns></returns>
        /// <exception cref="TraitNotFoundException"></exception>
        public float GetFloatTrait(EntityID entityId, string trait)
        {
            if (TryGetTrait(entityId, trait, out float value))
            {
                return value;
            }

            throw new TraitNotFoundException($"Entity does not have trait: {trait}");
        }

        /// <summary>
        /// Get the string value of a trait.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <returns></returns>
        /// <exception cref="TraitNotFoundException"></exception>
        public string? GetStringTrait(EntityID entityId, string trait)
        {
            if (TryGetTrait(entityId, trait, out string? value))
            {
                return value;
            }

            throw new TraitNotFoundException($"Entity does not have trait: {trait}");
        }

        /// <summary>
        /// Get the boolean value of a trait.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <returns></returns>
        /// <exception cref="TraitNotFoundException"></exception>
        public bool GetBoolTrait(EntityID entityId, string trait)
        {
            if (TryGetTrait(entityId, trait, out bool value))
            {
                return value;
            }

            throw new TraitNotFoundException($"Entity does not have trait: {trait}");
        }

        /// <summary>
        /// Remove the given trait from the entity.
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        public void RemoveTrait(EntityID entityId, string trait)
        {
            if (m_columns.TryGetValue(trait, out var col))
            {
                col.Remove(entityId);
            }
        }

        /// <summary>
        /// Set the value of a trait.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <param name="value"></param>
        public void SetTrait<T>(EntityID entityId, string trait, T value)
        {
            GetOrCreateTraitColumn<T>(trait).Set(entityId, value);
        }

        /// <summary>
        /// Check if an entity has a given trait
        /// </summary>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <returns></returns>
        public bool HasTrait(EntityID entityId, string trait)
        {
            if (m_columns.TryGetValue(trait, out var col))
            {
                return col.Contains(entityId);
            }

            return false;
        }

        /// <summary>
        /// Attempt to get the value of the given trait.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entityId"></param>
        /// <param name="trait"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetTrait<T>(EntityID entityId, string trait, out T? value)
        {
            value = default;
            return m_columns.TryGetValue(trait, out var col)
                && col is Column<T> typed
                && typed.TryGet(entityId, out value);
        }

        /// <summary>
        /// Get the column object corresponding to the given trait name.
        /// This method will create a new column if one does not already exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="trait"></param>
        /// <returns></returns>
        /// <exception cref="TraitTypeMismatchException"></exception>
        private Column<T> GetOrCreateTraitColumn<T>(string trait)
        {
            if (!m_columns.TryGetValue(trait, out var col))
            {
                col = new Column<T>();
                m_columns.Add(trait, col);
            }

            try
            {
                return (Column<T>)col;
            }
            catch (InvalidCastException)
            {
                throw new TraitTypeMismatchException();
            }
        }

        /// <summary>
        /// Destroys incoming and outgoing relationship edges for the given
        /// entity.
        /// </summary>
        /// <param name="entityId"></param>
        private void DestroyAllRelationshipsForEntity(EntityID entityId)
        {
            // Remove all outgoing edges
            if (m_outgoingEdges.TryGetValue(entityId, out var outgoingEdgeIds))
            {
                for (int i = outgoingEdgeIds.Count - 1; i >= 0; i--)
                {
                    EntityID relationshipId = outgoingEdgeIds[i];
                    DestroyRelationship(relationshipId);
                }
                m_outgoingEdges.Remove(entityId);
            }

            // Remove all incoming edges
            if (m_incomingEdges.TryGetValue(entityId, out var incomingEdgeIds))
            {
                for (int i = incomingEdgeIds.Count - 1; i >= 0; i--)
                {
                    EntityID relationshipId = incomingEdgeIds[i];
                    DestroyRelationship(relationshipId);
                }
                m_outgoingEdges.Remove(entityId);
            }
        }

        /// <summary>
        /// Removes the given entity from all trait columns where it
        /// stores values.
        /// </summary>
        /// <param name="entityId"></param>
        private void DeleteAllTraitsForEntity(EntityID entityId)
        {
            foreach (var col in m_columns.Values)
            {
                col.Remove(entityId);
            }
        }

        /// <summary>
        /// A utility method that adds a relationship to a dictionary mapping entities to
        /// it's incoming or outgoing relationships.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="entityId"></param>
        /// <param name="relationshipId"></param>
        private static void AddRelationshipDictEntry(
            Dictionary<EntityID, List<EntityID>> dict,
            EntityID entityId,
            EntityID relationshipId)
        {
            if (!dict.TryGetValue(entityId, out var edgeList))
            {
                edgeList = new List<EntityID>();
                dict.Add(entityId, edgeList);
            }

            edgeList.Add(relationshipId);
        }

        /// <summary>
        /// A utility method that removes a relationship from a dictionary mapping
        /// entities to their incoming or outgoing relationship edges.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="relationshipId"></param>
        private static void RemoveRelationshipDictEntry(
            Dictionary<EntityID, List<EntityID>> dict,
            EntityID relationshipId)
        {
            foreach (var relationshipIdList in dict.Values)
            {
                if (relationshipIdList != null)
                {
                    relationshipIdList.Remove(relationshipId);
                }
            }
        }

        /// <summary>
        /// Get the next free index in the source and target arrays for
        /// relationship endpoints.
        /// </summary>
        /// <returns></returns>
        private int GetFreeSourceTargetIndex()
        {
            if (m_freeSourceTargetIndices.Count > 0)
            {
                return m_freeSourceTargetIndices.Dequeue();
            }

            if (m_nextSourceTargetIndex == m_sources.Length)
            {
                IncreaseSourceTargetArrayLengths();
            }

            int index = m_nextSourceTargetIndex++;
            return index;
        }

        /// <summary>
        /// Increase the sizes of the source and target arrays when the number
        /// of active relationships grows beyond the the sizes of these arrays.
        /// </summary>
        private void IncreaseSourceTargetArrayLengths()
        {
            Array.Resize(ref m_sources, m_sources.Length + s_columnSizeIncrement);
            Array.Resize(ref m_targets, m_targets.Length + s_columnSizeIncrement);
        }
    }
} // namespace ERGraph