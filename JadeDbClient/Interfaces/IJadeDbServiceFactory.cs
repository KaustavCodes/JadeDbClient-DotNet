namespace JadeDbClient.Interfaces;

/// <summary>
/// A factory that provides named <see cref="IDatabaseService"/> instances, enabling multiple database connections.
/// </summary>
public interface IJadeDbServiceFactory
{
    /// <summary>
    /// Retrieves the <see cref="IDatabaseService"/> registered under the specified name.
    /// </summary>
    /// <param name="name">The name used when the connection was registered.</param>
    /// <returns>The <see cref="IDatabaseService"/> for the named connection.</returns>
    /// <exception cref="System.Collections.Generic.KeyNotFoundException">
    /// Thrown when no connection is registered with the given name.
    /// </exception>
    IDatabaseService GetService(string name);

    /// <summary>
    /// Retrieves the default <see cref="IDatabaseService"/>.
    /// </summary>
    /// <returns>The default <see cref="IDatabaseService"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no default connection has been designated via
    /// <see cref="JadeDbClient.Initialize.JadeDbNamedConnectionsBuilder.SetDefaultConnection"/> or the
    /// <c>JadeDb:DefaultConnection</c> configuration key.
    /// </exception>
    IDatabaseService GetService();
}
