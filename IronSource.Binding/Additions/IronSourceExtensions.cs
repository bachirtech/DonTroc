using System;

#nullable enable

namespace IronSource.SDK
{
    /// <summary>
    /// Classe pour stocker les informations de récompense IronSource
    /// </summary>
    public class RewardInfo
    {
        /// <summary>Nom de la récompense</summary>
        public string Name { get; set; }
        
        /// <summary>Montant de la récompense</summary>
        public int Amount { get; set; }

        public RewardInfo()
        {
            Name = "reward";
            Amount = 1;
        }

        public RewardInfo(string name, int amount)
        {
            Name = name;
            Amount = amount;
        }
    }
}
