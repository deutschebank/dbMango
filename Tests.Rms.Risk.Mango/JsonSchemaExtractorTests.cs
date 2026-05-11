namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class JsonSchemaExtractorTests
{
    [Test]
    public async Task InferJsonSchema_RepeatedSubObject_ExtractsDefAndRefs()
    {
        var docs = new[]
        {
            new BsonDocument
            {
                ["a"] = new BsonDocument { ["x"] = 1, ["y"] = "foo" },
                ["b"] = new BsonDocument { ["x"] = 2, ["y"] = "bar" }
            },
            new BsonDocument
            {
                ["a"] = new BsonDocument { ["x"] = 3, ["y"] = "baz" },
                ["b"] = new BsonDocument { ["x"] = 4, ["y"] = "qux" }
            }
        };

        var schema = await JsonSchemaExtractor.InferJsonSchema(CreateService(docs), sampleSize: 10);

        Assert.IsTrue(schema.Contains("$defs"));
        var defs = schema["$defs"].AsBsonDocument;
        Assert.AreEqual(1, defs.ElementCount);

        var props = schema["properties"].AsBsonDocument;
        Assert.IsTrue(props["a"].AsBsonDocument.Contains("$ref"));
        Assert.IsTrue(props["b"].AsBsonDocument.Contains("$ref"));
        Assert.AreEqual(props["a"]["$ref"], props["b"]["$ref"]);
    }

    [Test]
    public async Task InferJsonSchema_SingleOccurrence_DoesNotCreateDefs()
    {
        var docs = new[]
        {
            new BsonDocument
            {
                ["a"] = new BsonDocument { ["x"] = 1, ["y"] = "foo" },
                ["z"] = 10
            }
        };

        var schema = await JsonSchemaExtractor.InferJsonSchema(CreateService(docs), sampleSize: 10);

        Assert.IsFalse(schema.Contains("$defs"));
    }

    [Test]
    public async Task InferJsonSchema_UnionLikeReuse_ObjectExtractedAcrossObjectAndArrayItems()
    {
        var docs = new[]
        {
            new BsonDocument
            {
                ["a"] = new BsonDocument { ["x"] = 1 },
                ["arr"] = new BsonArray { new BsonDocument { ["x"] = 2 } }
            },
            new BsonDocument
            {
                ["a"] = new BsonDocument { ["x"] = 3 },
                ["arr"] = new BsonArray { new BsonDocument { ["x"] = 4 } }
            }
        };

        var schema = await JsonSchemaExtractor.InferJsonSchema(CreateService(docs), sampleSize: 10);

        Assert.IsTrue(schema.Contains("$defs"));

        var props = schema["properties"].AsBsonDocument;
        var aRef = props["a"].AsBsonDocument["$ref"].AsString;
        var arrItemsRef = props["arr"].AsBsonDocument["items"].AsBsonDocument["$ref"].AsString;
        Assert.AreEqual(aRef, arrItemsRef);
    }

    [Test]
    public async Task InferJsonSchema_DeterministicDefNaming_IsStableAcrossPropertyOrder()
    {
        var docs1 = new[]
        {
            new BsonDocument
            {
                ["a"] = new BsonDocument { ["x"] = 1, ["y"] = "foo" },
                ["b"] = new BsonDocument { ["x"] = 2, ["y"] = "bar" }
            }
        };

        var docs2 = new[]
        {
            new BsonDocument
            {
                ["b"] = new BsonDocument { ["y"] = "bar", ["x"] = 2 },
                ["a"] = new BsonDocument { ["y"] = "foo", ["x"] = 1 }
            }
        };

        var schema1 = await JsonSchemaExtractor.InferJsonSchema(CreateService(docs1), sampleSize: 10);
        var schema2 = await JsonSchemaExtractor.InferJsonSchema(CreateService(docs2), sampleSize: 10);

        Assert.AreEqual(schema1.ToJson(), schema2.ToJson());
    }

    [Test]
    public async Task InferJsonSchema_DeepNextChain_DoesNotOverflow()
    {
        var root = new BsonDocument();
        var current = root;
        for (var i = 0; i < 200; i++)
        {
            var next = new BsonDocument();
            current["next"] = next;
            current = next;
        }
        current["value"] = 1;

        Assert.DoesNotThrowAsync(async () =>
        {
            var schema = await JsonSchemaExtractor.InferJsonSchema(CreateService([root]), sampleSize: 10);
            Assert.IsNotNull(schema);
        });
    }

    private static IMongoDbService<BsonDocument> CreateService(IEnumerable<BsonDocument> docs)
    {
        var mock = new Mock<IMongoDbService<BsonDocument>>();
        mock.Setup(x => x.CollectionName).Returns("testCollection");
        mock.Setup(x => x.AggregateAsyncRaw(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(ToAsyncEnumerable(docs));

        return mock.Object;
    }

    private static async IAsyncEnumerable<BsonDocument> ToAsyncEnumerable(IEnumerable<BsonDocument> docs)
    {
        foreach (var doc in docs)
        {
            await Task.Yield();
            yield return doc;
        }
    }
}
