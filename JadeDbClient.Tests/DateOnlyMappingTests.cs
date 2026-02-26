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
