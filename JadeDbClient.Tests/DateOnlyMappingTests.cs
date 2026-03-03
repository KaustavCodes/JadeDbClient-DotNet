using System;
using System.Data;
using FluentAssertions;
using JadeDbClient.Helpers;
using JadeDbClient.Initialize;
using Moq;
using Xunit;

namespace JadeDbClient.Tests;

/// <summary>
/// Tests to verify DateOnly / DateTime interoperability in mapper
/// </summary>
public class DateOnlyMappingTests
{
    /// <summary>
    /// Reflection mapper: DB returns DateTime, property is DateOnly → should convert automatically.
    /// </summary>
    [Fact]
    public void ReflectionMapper_DateTimeFromDb_MapsToDateOnlyProperty()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.FieldCount).Returns(2);
        mockReader.Setup(r => r.GetName(0)).Returns("Id");
        mockReader.Setup(r => r.GetName(1)).Returns("BirthDate");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        mockReader.Setup(r => r[0]).Returns(1);
        mockReader.Setup(r => r[1]).Returns(new DateTime(2000, 6, 15)); // DB returns DateTime

        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);

        // Act
        var result = mapper.MapObjectReflection<PersonWithDateOnly>(mockReader.Object);

        // Assert
        result.Id.Should().Be(1);
        result.BirthDate.Should().Be(new DateOnly(2000, 6, 15));
    }

    /// <summary>
    /// Reflection mapper: DB returns DateOnly, property is DateTime → should convert automatically.
    /// </summary>
    [Fact]
    public void ReflectionMapper_DateOnlyFromDb_MapsToDateTimeProperty()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.FieldCount).Returns(2);
        mockReader.Setup(r => r.GetName(0)).Returns("Id");
        mockReader.Setup(r => r.GetName(1)).Returns("CreatedAt");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        mockReader.Setup(r => r[0]).Returns(2);
        mockReader.Setup(r => r[1]).Returns(new DateOnly(2023, 3, 10)); // DB returns DateOnly

        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);

        // Act
        var result = mapper.MapObjectReflection<PersonWithDateTime>(mockReader.Object);

        // Assert
        result.Id.Should().Be(2);
        result.CreatedAt.Should().Be(new DateTime(2023, 3, 10, 0, 0, 0));
    }

    /// <summary>
    /// Reflection mapper: nullable DateOnly property with DateTime from DB.
    /// </summary>
    [Fact]
    public void ReflectionMapper_DateTimeFromDb_MapsToNullableDateOnlyProperty()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.FieldCount).Returns(2);
        mockReader.Setup(r => r.GetName(0)).Returns("Id");
        mockReader.Setup(r => r.GetName(1)).Returns("BirthDate");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        mockReader.Setup(r => r[0]).Returns(3);
        mockReader.Setup(r => r[1]).Returns(new DateTime(1990, 12, 25));

        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);

        // Act
        var result = mapper.MapObjectReflection<PersonWithNullableDateOnly>(mockReader.Object);

        // Assert
        result.Id.Should().Be(3);
        result.BirthDate.Should().Be(new DateOnly(1990, 12, 25));
    }

    /// <summary>
    /// Reflection mapper: nullable DateOnly property with DBNull → should remain null.
    /// </summary>
    [Fact]
    public void ReflectionMapper_DBNull_NullableDateOnlyRemainsNull()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.FieldCount).Returns(2);
        mockReader.Setup(r => r.GetName(0)).Returns("Id");
        mockReader.Setup(r => r.GetName(1)).Returns("BirthDate");
        mockReader.Setup(r => r.IsDBNull(0)).Returns(false);
        mockReader.Setup(r => r.IsDBNull(1)).Returns(true); // NULL in DB
        mockReader.Setup(r => r[0]).Returns(4);

        var mapperOptions = new JadeDbMapperOptions();
        var mapper = new Mapper(mapperOptions);

        // Act
        var result = mapper.MapObjectReflection<PersonWithNullableDateOnly>(mockReader.Object);

        // Assert
        result.Id.Should().Be(4);
        result.BirthDate.Should().BeNull();
    }

    /// <summary>
    /// Simulates the mapper registration pattern that the source generator would produce for a DateOnly property:
    /// the generator emits DateOnly.FromDateTime(reader.GetDateTime(...)), which is verified here.
    /// </summary>
    [Fact]
    public void SourceGeneratorMapper_DateOnly_MappedCorrectly()
    {
        // Arrange
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.GetOrdinal("Id")).Returns(0);
        mockReader.Setup(r => r.GetOrdinal("EventDate")).Returns(1);
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        mockReader.Setup(r => r.GetInt32(0)).Returns(10);
        mockReader.Setup(r => r.GetDateTime(1)).Returns(new DateTime(2025, 1, 20));

        // Simulate the mapper that the source generator emits for a DateOnly property
        JadeDbMapperOptions.RegisterGlobalMapper<EventModel>(reader => new EventModel
        {
            Id = reader.GetInt32(reader.GetOrdinal("Id")),
            EventDate = DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("EventDate")))
        });

        var mapperOptions = new JadeDbMapperOptions();

        // Act
        var result = mapperOptions.ExecuteMapper<EventModel>(mockReader.Object);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(10);
        result.EventDate.Should().Be(new DateOnly(2025, 1, 20));
    }

    /// <summary>
    /// Simulates the mapper registration pattern that the source generator would produce for a DateTimeOffset
    /// property when the DB provider returns a DateTimeOffset value directly (e.g. SQL Server datetimeoffset).
    /// The generator emits: reader.GetValue(ord) is DateTimeOffset dto ? dto : new DateTimeOffset(reader.GetDateTime(ord))
    /// </summary>
    [Fact]
    public void SourceGeneratorMapper_DateTimeOffset_WhenDbReturnsDateTimeOffset_MappedCorrectly()
    {
        // Arrange
        var expected = new DateTimeOffset(2025, 6, 15, 10, 30, 0, TimeSpan.FromHours(5));
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.FieldCount).Returns(2);
        mockReader.Setup(r => r.GetName(0)).Returns("Id");
        mockReader.Setup(r => r.GetName(1)).Returns("CreatedAt");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        // DB provider returns DateTimeOffset from GetValue
        mockReader.Setup(r => r.GetValue(1)).Returns(expected);

        // Simulate the mapper emitted by the source generator for a DateTimeOffset property
        JadeDbMapperOptions.RegisterGlobalMapper<EventWithDateTimeOffset>(reader =>
        {
            int __ord_Id = -1, __ord_CreatedAt = -1;
            for (int __i = 0; __i < reader.FieldCount; __i++)
            {
                var __cn = reader.GetName(__i);
                if (string.Equals(__cn, "Id",        StringComparison.OrdinalIgnoreCase)) { __ord_Id        = __i; continue; }
                if (string.Equals(__cn, "CreatedAt", StringComparison.OrdinalIgnoreCase)) { __ord_CreatedAt = __i; continue; }
            }
            return new EventWithDateTimeOffset
            {
                Id        = __ord_Id        >= 0 ? reader.GetInt32(__ord_Id) : default,
                CreatedAt = __ord_CreatedAt >= 0 ? (reader.GetValue(__ord_CreatedAt) is DateTimeOffset __dto_CreatedAt ? __dto_CreatedAt : new DateTimeOffset(reader.GetDateTime(__ord_CreatedAt))) : default,
            };
        });

        var mapperOptions = new JadeDbMapperOptions();

        // Act
        var result = mapperOptions.ExecuteMapper<EventWithDateTimeOffset>(mockReader.Object);

        // Assert
        result.Should().NotBeNull();
        result!.CreatedAt.Should().Be(expected);
        result.CreatedAt.Offset.Should().Be(TimeSpan.FromHours(5));
    }

    /// <summary>
    /// Simulates the mapper registration pattern that the source generator would produce for a DateTimeOffset
    /// property when the DB provider returns a plain DateTime (e.g. MySQL, SQLite).
    /// The generator emits: reader.GetValue(ord) is DateTimeOffset dto ? dto : new DateTimeOffset(reader.GetDateTime(ord))
    /// </summary>
    [Fact]
    public void SourceGeneratorMapper_DateTimeOffset_WhenDbReturnsDateTime_FallsBackToConversion()
    {
        // Arrange
        var dateTime = new DateTime(2025, 3, 1, 8, 0, 0);
        var mockReader = new Mock<IDataReader>();
        mockReader.Setup(r => r.FieldCount).Returns(2);
        mockReader.Setup(r => r.GetName(0)).Returns("Id");
        mockReader.Setup(r => r.GetName(1)).Returns("CreatedAt");
        mockReader.Setup(r => r.IsDBNull(It.IsAny<int>())).Returns(false);
        // DB provider returns DateTime from GetValue (fallback path)
        mockReader.Setup(r => r.GetValue(1)).Returns(dateTime);
        mockReader.Setup(r => r.GetDateTime(1)).Returns(dateTime);

        JadeDbMapperOptions.RegisterGlobalMapper<EventWithDateTimeOffset2>(reader =>
        {
            int __ord_Id = -1, __ord_CreatedAt = -1;
            for (int __i = 0; __i < reader.FieldCount; __i++)
            {
                var __cn = reader.GetName(__i);
                if (string.Equals(__cn, "Id",        StringComparison.OrdinalIgnoreCase)) { __ord_Id        = __i; continue; }
                if (string.Equals(__cn, "CreatedAt", StringComparison.OrdinalIgnoreCase)) { __ord_CreatedAt = __i; continue; }
            }
            return new EventWithDateTimeOffset2
            {
                Id        = __ord_Id        >= 0 ? reader.GetInt32(__ord_Id) : default,
                CreatedAt = __ord_CreatedAt >= 0 ? (reader.GetValue(__ord_CreatedAt) is DateTimeOffset __dto_CreatedAt ? __dto_CreatedAt : new DateTimeOffset(reader.GetDateTime(__ord_CreatedAt))) : default,
            };
        });

        var mapperOptions = new JadeDbMapperOptions();

        // Act
        var result = mapperOptions.ExecuteMapper<EventWithDateTimeOffset2>(mockReader.Object);

        // Assert
        result.Should().NotBeNull();
        result!.CreatedAt.DateTime.Should().Be(dateTime);
    }
}

// Test models
public class PersonWithDateOnly
{
    public int Id { get; set; }
    public DateOnly BirthDate { get; set; }
}

public class PersonWithDateTime
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PersonWithNullableDateOnly
{
    public int Id { get; set; }
    public DateOnly? BirthDate { get; set; }
}

public class EventModel
{
    public int Id { get; set; }
    public DateOnly EventDate { get; set; }
}

public class EventWithDateTimeOffset
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

// Separate class required: RegisterGlobalMapper<T> is keyed by type,
// so each test that registers a mapper needs a distinct model class.
public class EventWithDateTimeOffset2
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
