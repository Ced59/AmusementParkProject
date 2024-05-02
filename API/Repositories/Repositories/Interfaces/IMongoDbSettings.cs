namespace Repositories.Interfaces;

public interface IMongoDbSettings
{
    string Url { get; }
    string DatabaseName { get; }
    string Username { get; }
    string Password { get; }
    string UsersCollectionName { get; }
}