namespace MineTimbermanBot.Application.Sessions;

public interface IUserSessionStore
{
    UserSession GetOrCreate(long userId);
}
