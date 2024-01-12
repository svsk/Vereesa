using System;
using System.Collections.Generic;

namespace Vereesa.Neon.Data.Models.Gambling
{
    public class GamblingRound
    {
        public int MaxValue { get; set; }
        public Dictionary<ulong, int?> Rolls { get; set; }
        public bool AwaitingRolls { get; set; }

        public static GamblingRound CreateInstance(int maxValue)
        {
            return new GamblingRound
            {
                MaxValue = maxValue,
                Rolls = new Dictionary<ulong, int?>(),
                AwaitingRolls = false
            };
        }
    }
}
