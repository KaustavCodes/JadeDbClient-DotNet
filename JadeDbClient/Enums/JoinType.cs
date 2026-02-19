using System;

namespace JadeDbClient.Enums;

/// <summary>
/// Specifies the type of SQL JOIN to use when joining tables in a QueryBuilder query.
/// </summary>
public enum JoinType
{
    /// <summary>Returns rows that have matching values in both tables.</summary>
    Inner,

    /// <summary>Returns all rows from the left (main) table and matching rows from the right (joined) table.</summary>
    Left,

    /// <summary>Returns all rows from the right (joined) table and matching rows from the left (main) table.</summary>
    Right,

    /// <summary>Returns all rows from both tables, with NULLs where there is no match.</summary>
    Full
}
