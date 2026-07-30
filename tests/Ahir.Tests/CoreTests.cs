using Ahir.Core.Utilities;
using Ahir.Core.Models;

namespace Ahir.Tests;

public class CoreTests
{
    [Fact]
    public void IdGenerator_NewId_ReturnsUniqueIds()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => IdGenerator.NewId()).ToList();
        Assert.Equal(100, ids.Distinct().Count());
        Assert.All(ids, id => Assert.False(string.IsNullOrEmpty(id)));
    }

    [Fact]
    public void IdGenerator_NewShortId_Returns8CharBase62()
    {
        var id = IdGenerator.NewShortId();
        Assert.False(string.IsNullOrEmpty(id));
    }

    [Fact]
    public void Checksum_Compute_ReturnsConsistentHash()
    {
        var data = "test data"u8.ToArray();
        var hash1 = Checksum.Compute(data);
        var hash2 = Checksum.Compute(data);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Checksum_Verify_ReturnsTrueForMatchingHash()
    {
        var data = "verify me"u8.ToArray();
        var hash = Checksum.Compute(data);
        Assert.True(Checksum.Verify(data, hash));
    }

    [Fact]
    public void Checksum_DifferentInput_DifferentHash()
    {
        var hash1 = Checksum.Compute("hello"u8.ToArray());
        var hash2 = Checksum.Compute("world"u8.ToArray());
        Assert.NotEqual(hash1, hash2);
    }
}

public class ModelTests
{
    [Fact]
    public void AhirResult_Ok_SetsSuccess()
    {
        var result = AhirResult<string>.Ok("test");
        Assert.True(result.Success);
        Assert.Equal("test", result.Data);
    }

    [Fact]
    public void AhirResult_Fail_SetsError()
    {
        var result = AhirResult<int>.Fail("ERR", "Something went wrong");
        Assert.False(result.Success);
        Assert.Equal("ERR", result.ErrorCode);
        Assert.Equal("Something went wrong", result.ErrorMessage);
    }

    [Fact]
    public void PageResult_HasNext_CalculatesCorrectly()
    {
        var page = new PageResult<string> { Items = ["a", "b"], TotalCount = 50, Page = 1, PageSize = 10 };
        Assert.True(page.HasNext);
        Assert.False(page.HasPrevious);

        var lastPage = new PageResult<string> { Items = [], TotalCount = 5, Page = 1, PageSize = 10 };
        Assert.False(lastPage.HasNext);
    }

    [Fact]
    public void AhirRecord_GetField_ReturnsTypedValue()
    {
        var record = new AhirRecord { Fields = new() { { "age", 25 }, { "name", "test" } } };
        Assert.Equal(25, record.GetField<int>("age"));
        Assert.Equal("test", record.GetField<string>("name"));
        Assert.Null(record.GetField<string>("nonexistent"));
    }

    [Fact]
    public void AhirRecord_HasField_Works()
    {
        var record = new AhirRecord { Fields = new() { { "key", "value" } } };
        Assert.True(record.HasField("key"));
        Assert.False(record.HasField("missing"));
    }
}

public class GuardTests
{
    [Fact]
    public void NotNull_ValidInput_DoesNotThrow()
    {
        var ex = Record.Exception(() => Guard.NotNull("test", "value"));
        Assert.Null(ex);
    }

    [Fact]
    public void NotNull_NullInput_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Guard.NotNull<object>(null!, "value"));
    }

    [Fact]
    public void NotNullOrEmpty_ValidString_DoesNotThrow()
    {
        var ex = Record.Exception(() => Guard.NotNullOrEmpty("test", "value"));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void NotNullOrEmpty_InvalidString_Throws(string? input)
    {
        Assert.Throws<ArgumentException>(() => Guard.NotNullOrEmpty(input!, "value"));
    }

    [Fact]
    public void NotNullOrWhiteSpace_Whitespace_Throws()
    {
        Assert.Throws<ArgumentException>(() => Guard.NotNullOrWhiteSpace("   ", "value"));
    }

    [Fact]
    public void ValidDatabaseName_TooLong_Throws()
    {
        var name = new string('a', 65);
        Assert.Throws<ArgumentException>(() => Guard.ValidDatabaseName(name));
    }

    [Fact]
    public void ValidDatabaseName_Valid_DoesNotThrow()
    {
        var ex = Record.Exception(() => Guard.ValidDatabaseName("my-db_123"));
        Assert.Null(ex);
    }

    [Fact]
    public void Positive_ValidValue_DoesNotThrow()
    {
        var ex = Record.Exception(() => Guard.Positive(5, "value"));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Positive_InvalidValue_Throws(int value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Positive(value, "value"));
    }
}
