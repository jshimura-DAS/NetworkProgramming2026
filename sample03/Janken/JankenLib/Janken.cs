namespace JankenLib;

/// <summary>
/// じゃんけんの手の種類
/// </summary>
public enum Hand
{
    Rock = 0,      // グー
    Paper = 1,     // パー
    Scissors = 2   // チョキ
}

/// <summary>
/// じゃんけんの結果
/// </summary>
public enum Result
{
    Win,    // 勝ち
    Lose,   // 負け
    Draw    // 引き分け
}

/// <summary>
/// じゃんけんゲームのロジッククラス
/// </summary>
public class Janken
{
    /// <summary>
    /// 手の名前を取得
    /// </summary>
    public static string GetHandName(Hand hand)
    {
        return hand switch
        {
            Hand.Rock => "グー",
            Hand.Paper => "パー",
            Hand.Scissors => "チョキ",
            _ => "不明"
        };
    }

    /// <summary>
    /// 文字列から手を取得
    /// </summary>
    public static Hand? ParseHand(string input)
    {
        return input.ToLower() switch
        {
            "rock" or "グー" or "0" => Hand.Rock,
            "paper" or "パー" or "1" => Hand.Paper,
            "scissors" or "チョキ" or "2" => Hand.Scissors,
            _ => null
        };
    }

    /// <summary>
    /// じゃんけんの勝敗判定
    /// </summary>
    /// <param name="player1">プレイヤー1の手</param>
    /// <param name="player2">プレイヤー2の手</param>
    /// <returns>プレイヤー1から見た結果</returns>
    public static Result Judge(Hand player1, Hand player2)
    {
        if (player1 == player2)
        {
            return Result.Draw;
        }

        return (player1, player2) switch
        {
            (Hand.Rock, Hand.Scissors) => Result.Win,
            (Hand.Paper, Hand.Rock) => Result.Win,
            (Hand.Scissors, Hand.Paper) => Result.Win,
            _ => Result.Lose
        };
    }

    /// <summary>
    /// 結果の文字列表現を取得
    /// </summary>
    public static string GetResultMessage(Result result)
    {
        return result switch
        {
            Result.Win => "勝ち",
            Result.Lose => "負け",
            Result.Draw => "引き分け",
            _ => "不明"
        };
    }

    /// <summary>
    /// ランダムな手を生成
    /// </summary>
    public static Hand GetRandomHand()
    {
        Random random = new Random();
        return (Hand)random.Next(0, 3);
    }
}
