using ERGraph.Errors;
using NUnit.Framework;

namespace ERGraph.Test
{
    public class CoreTests
    {
        /// <summary>
        /// Test that entities properly exist after creation.
        /// </summary>
        [Test]
        public void TestCreateEntity()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            Assert.That(db.EntityExists(entityA), Is.True);
        }

        /// <summary>
        /// Test assigning a name to an entity at creation time.
        /// </summary>
        [Test]
        public void TestCreateNamedEntity()
        {
            var db = new Database();
            var entityA = db.CreateEntity("A");
            Assert.That(db.EntityExists(entityA), Is.True);
        }

        /// <summary>
        /// Test that using a non unique name results in throwing a
        /// duplicate name exception.
        /// </summary>
        [Test]
        public void TestCreateNamedEntityThrowsDuplicationError()
        {
            var db = new Database();
            var entityA = db.CreateEntity("A");

            Assert.Throws(typeof(DuplicateNameException), () =>
            {
                db.CreateEntity("A");
            });
        }

        /// <summary>
        /// Test retrieving an entity using its name.
        /// </summary>
        [Test]
        public void TestGetEntityByName()
        {
            var db = new Database();
            var entityA = db.CreateEntity("A");
            Assert.That(db.GetEntityByName("A"), Is.EqualTo(entityA));
        }

        /// <summary>
        /// Test attempting to retrieve an entity using its name
        /// </summary>
        [Test]
        public void TestTryGetEntityByName()
        {
            var db = new Database();
            Assert.That(db.TryGetEntityByName("A", out var _), Is.False);
            db.CreateEntity("A");
            Assert.That(db.TryGetEntityByName("A", out var _), Is.True);
        }

        /// <summary>
        /// Test setting the entity name.
        /// </summary>
        [Test]
        public void TestSetEntityName()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            Assert.That(db.TryGetEntityByName("A", out var _), Is.False);
            db.SetEntityName(entityA, "A");
            Assert.That(db.TryGetEntityByName("A", out var _), Is.True);
        }

        /// <summary>
        /// Test that using a non-unique name with .SetEntityName()
        /// throws a duplicate name exception.
        /// </summary>
        [Test]
        public void TestSetEntityNameThrowsDuplicateError()
        {
            var db = new Database();
            var entityA = db.CreateEntity("A");
            db.SetEntityName(entityA, "A");

            Assert.Throws(typeof(DuplicateNameException), () =>
            {
                var entityB = db.CreateEntity();
                db.SetEntityName(entityB, "A");
            });
        }

        /// <summary>
        /// Test that entities no longer exist in the database after destruction.
        /// </summary>
        [Test]
        public void TestEntityDoesNotExistAfterDestroy()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            db.DestroyEntity(entityA);
            Assert.That(db.EntityExists(entityA), Is.False);
        }

        /// <summary>
        /// Test that destroying an entity also destroys its traits.
        /// </summary>
        [Test]
        public void TestDestroyEntityRemovesTraitsData()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            db.SetFlagTrait(entityA, "has_magic_sword");

            Assert.That(db.HasTrait(entityA, "has_magic_sword"), Is.True);

            db.DestroyEntity(entityA);

            Assert.That(
                db.TryGetTrait(entityA, "has_magic_sword", out bool _), Is.False);
        }

        /// <summary>
        /// Test that destroying an entity also destroys its relationships.
        /// </summary>
        [Test]
        public void TestDestroyEntityDestroysRelationships()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            var entityC = db.CreateEntity();
            var relAToB = db.CreateRelationship(entityA, entityB);
            var relBToC = db.CreateRelationship(entityB, entityC);

            db.DestroyEntity(entityB);

            Assert.That(db.RelationshipExists(relAToB), Is.False);
            Assert.That(db.RelationshipExists(relBToC), Is.False);
        }

        /// <summary>
        /// Test that destroying an entity allows it's ID to be recycled to the
        /// next created entity.
        /// </summary>
        [Test]
        public void TestDestroyEntityRecyclesId()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            db.DestroyEntity(entityA);
            var entityB = db.CreateEntity();
            Assert.That(entityB, Is.EqualTo(entityA));
        }

        /// <summary>
        /// Test that destroying an entity that does not exist returns false
        /// instead of throwing an exception.
        /// </summary>
        [Test]
        public void DestroyEntityFailsQuietly()
        {
            var db = new Database();
            Assert.That(db.DestroyEntity(0), Is.False);
        }

        /// <summary>
        /// Test that Database.DestroyEntity() can be used to destroy
        /// relationships using their ID.
        /// </summary>
        [Test]
        public void TestDestroyEntityWorksForRelationships()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            var relAToB = db.CreateRelationship(entityA, entityB);
            Assert.That(db.DestroyEntity(relAToB), Is.True);
        }

        /// <summary>
        /// Test that a relationship exists after creation.
        /// </summary>
        [Test]
        public void TestCreateRelationship()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            var relAToB = db.CreateRelationship(entityA, entityB);
            Assert.That(db.RelationshipExists(entityA, entityB), Is.True);
            Assert.That(db.RelationshipExists(relAToB), Is.True);
        }

        /// <summary>
        /// Test destroying a relationship using its ID.
        /// </summary>
        [Test]
        public void TestDestroyRelationshipById()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            var relAToB = db.CreateRelationship(entityA, entityB);

            Assert.That(db.RelationshipExists(relAToB), Is.True);

            db.DestroyRelationship(relAToB);

            Assert.That(db.RelationshipExists(relAToB), Is.False);
        }

        /// <summary>
        /// Test destroying a relationship using the IDs of its source and target.
        /// </summary>
        [Test]
        public void TestDestroyRelationshipByEndpoints()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            var relAToB = db.CreateRelationship(entityA, entityB);

            Assert.That(db.RelationshipExists(relAToB), Is.True);

            db.DestroyRelationship(entityA, entityB);

            Assert.That(db.RelationshipExists(relAToB), Is.False);
        }

        /// <summary>
        /// Test that all traits associated with a relationship edge are
        /// removed/deleted when the relationship is destroyed.
        /// </summary>
        [Test]
        public void TestDestroyRelationshipRemovesTraits()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            var relAToB = db.CreateRelationship(entityA, entityB);
            db.SetFlagTrait(relAToB, "friend_of");
            db.DestroyRelationship(relAToB);
            Assert.That(db.TryGetTrait(relAToB, "friend_of", out bool _), Is.False);
        }

        /// <summary>
        /// Test retrieving a relationship's ID using the IDs of its source and target.
        /// </summary>
        [Test]
        public void TestGetRelationship()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            var relAToB = db.CreateRelationship(entityA, entityB);

            Assert.That(db.GetRelationship(entityA, entityB), Is.EqualTo(relAToB));
        }

        /// <summary>
        /// Test attempting to retrieve a relationship ID using its source
        /// and target IDs.
        /// </summary>
        [Test]
        public void TestTryGetRelationship()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();

            Assert.That(
                db.TryGetRelationship(entityA, entityB, out var _), Is.False);

            db.CreateRelationship(entityA, entityB);

            Assert.That(
                db.TryGetRelationship(entityA, entityB, out var _), Is.True);
        }

        /// <summary>
        /// Test getting the endpoints for a given relationship.
        /// </summary>
        [Test]
        public void TestGetEndpoints()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            var relAToB = db.CreateRelationship(entityA, entityB);

            var endpoints = db.GetEndpoints(relAToB);

            Assert.That(endpoints.Item1, Is.EqualTo(entityA));
            Assert.That(endpoints.Item2, Is.EqualTo(entityB));
        }


        [Test]
        public void TestSetTrait()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            Assert.That(db.HasTrait(entityA, "flag"), Is.False);
            db.SetTrait(entityA, "flag", true);
            Assert.That(db.HasTrait(entityA, "flag"), Is.True);
        }

        [Test]
        public void TestHasTrait()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            Assert.That(db.HasTrait(entityA, "flag"), Is.False);
            db.SetFlagTrait(entityA, "flag");
            Assert.That(db.HasTrait(entityA, "flag"), Is.True);
        }

        [Test]
        public void TestSetTraitTypeMismatchError()
        {
            var db = new Database();
            var entityA = db.CreateEntity();
            var entityB = db.CreateEntity();
            db.SetIntTrait(entityA, "apples", 10);

            Assert.Throws(typeof(TraitTypeMismatchException), () =>
            {
                db.SetFloatTrait(entityB, "apples", 10f);
            });
        }

        [Test]
        public void TestRemoveTrait()
        {
            var db = new Database();
            var entityA = db.CreateEntity();

            db.SetFlagTrait(entityA, "flag");
            Assert.That(db.HasTrait(entityA, "flag"), Is.True);
            db.RemoveTrait(entityA, "flag");
            Assert.That(db.HasTrait(entityA, "flag"), Is.False);
        }

        [Test]
        public void TestTryGetTrait()
        {
            var db = new Database();
            var entityA = db.CreateEntity();

            Assert.That(db.TryGetTrait(entityA, "apples", out int _), Is.False);

            db.SetIntTrait(entityA, "apples", 10);

            Assert.That(db.TryGetTrait(entityA, "apples", out int _), Is.True);
        }
    }
} // namespace ERGraph.Test
