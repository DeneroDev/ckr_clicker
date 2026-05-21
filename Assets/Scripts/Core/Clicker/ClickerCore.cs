using System;
using Configs;

namespace Core.Clicker
{
    internal sealed class ClickerCore
    {
        private readonly ClickerBalanceConfig _config;
        private readonly ClickerModel _model = new();

        public ClickerCore(ClickerBalanceConfig config)
        {
            _config = config;
        }

        public int Currency => _model.Currency;
        public int Energy => _model.Energy;

        public void Initialize()
        {
            _model.Initialize(_config.InitialEnergy, _config.MaxEnergy);
        }

        public bool TryTapCollect()
        {
            return TryCollect(_config.TapEnergyCost, _config.TapCurrencyReward);
        }

        public bool TryAutoCollect()
        {
            return TryCollect(_config.AutoCollectEnergyCost, _config.AutoCollectCurrencyReward);
        }

        public void RegenerateEnergy()
        {
            _model.AddEnergy(_config.EnergyRegenAmount);
        }

        private bool TryCollect(int energyCost, int reward)
        {
            if (!_model.TrySpendEnergy(energyCost))
            {
                return false;
            }

            _model.AddCurrency(reward);
            return true;
        }

        private sealed class ClickerModel
        {
            public int Currency { get; private set; }
            public int Energy { get; private set; }
            public int MaxEnergy { get; private set; }

            public void Initialize(int initialEnergy, int maxEnergy)
            {
                Currency = 0;
                MaxEnergy = Math.Max(1, maxEnergy);
                Energy = Math.Clamp(initialEnergy, 0, MaxEnergy);
            }

            public bool TrySpendEnergy(int amount)
            {
                if (amount <= 0)
                {
                    return true;
                }

                if (Energy < amount)
                {
                    return false;
                }

                Energy -= amount;
                return true;
            }

            public void AddCurrency(int amount)
            {
                if (amount > 0)
                {
                    Currency += amount;
                }
            }

            public void AddEnergy(int amount)
            {
                if (amount <= 0)
                {
                    return;
                }

                Energy = Math.Clamp(Energy + amount, 0, MaxEnergy);
            }
        }
    }
}
