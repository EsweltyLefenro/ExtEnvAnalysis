using System.Collections.Generic;
using System.Linq;

namespace ExtEnvAnalysis.Core
{
    public static class PestelSeeds
    {
        // по 5 подсказок на тип
        public static readonly Dictionary<PestelType, string[]> Map = new()
        {
            [PestelType.P] = new[] { "Налоги", "Госрегулирование", "Лицензии", "Политстабильность", "Госзакупки" },
            [PestelType.E] = new[] { "Инфляция", "Курс валют", "Рынок труда", "Покупательская способность", "Ставки" },
            [PestelType.S] = new[] { "Демография", "Образ жизни", "Образование", "Культура", "Тренды" },
            [PestelType.T] = new[] { "Автоматизация", "AI/ML", "Кибербезопасность", "Облака", "Интеграции" },
            [PestelType.Env] = new[] { "Экостандарты", "Утилизация", "Энергопотребление", "CO₂", "Логистика" },
            [PestelType.L] = new[] { "IP/авторское", "Защита данных", "Трудовое право", "Контракты", "Стандарты" }
        };

        public static string[] For(PestelType t) =>
            Map.TryGetValue(t, out var a) && a?.Length == 5 ? a : Enumerable.Repeat("", 5).ToArray();
    }
}
