namespace MineTimbermanBot.Application.Duels;

public enum FightChoice
{
    Rock = 1,
    Paper = 2,
    Scissors = 3
}

public static class RpsChoiceExtensions
{
    public static string ToRussian(this FightChoice choice) => choice switch
    {
        FightChoice.Rock => "Бурилка(Камень)",
        FightChoice.Paper => "РудСтойка(Бумага)",
        FightChoice.Scissors => "Бензопила(Ножницы)",
        _ => choice.ToString()
    };

    public static bool TryParse(string value, out FightChoice choice)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "rock":
                choice = FightChoice.Rock;
                return true;
            case "paper":
                choice = FightChoice.Paper;
                return true;
            case "scissors":
                choice = FightChoice.Scissors;
                return true;
            default:
                choice = default;
                return false;
        }
    }

    public static FightChoice RandomChoice() => (FightChoice)Random.Shared.Next(1, 4);
    
    public static int Compare(FightChoice left, FightChoice right)
    {
        if (left == right)
        {
            return 0;
        }

        return (left, right) switch
        {
            (FightChoice.Rock, FightChoice.Scissors) => 1,
            (FightChoice.Paper, FightChoice.Rock) => 1,
            (FightChoice.Scissors, FightChoice.Paper) => 1,
            _ => -1
        };
    }
}
