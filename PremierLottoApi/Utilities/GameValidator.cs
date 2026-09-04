using System;
using System.Collections.Generic;
using System.Linq;

namespace PremierLottoApi.Utilities 
{
    public static class GameValidator
    {
        public static void ValidateGuessRanges(string gameType, List<string> guesses)
        {
            foreach (var guess in guesses)
            {
                if (gameType == "Easy")
                {
                    if (!int.TryParse(guess, out int val) || val < 0 || val > 30)
                        throw new ArgumentException($"Easy tier guesses must be numbers between 0 and 30. Invalid value: '{guess}'");
                }
                else if (gameType == "Classic")
                {
                    if (!int.TryParse(guess, out int val) || val < 0 || val > 60)
                        throw new ArgumentException($"Classic tier guesses must be numbers between 0 and 60. Invalid value: '{guess}'");
                }
                else if (gameType == "Pro")
                {
                    if (string.IsNullOrWhiteSpace(guess) || guess.Length > 5)
                        throw new ArgumentException($"Pro tier guesses must be valid alphanumeric values between 0 and 90. Invalid value: '{guess}'");
                }
            }
        }
    }
}