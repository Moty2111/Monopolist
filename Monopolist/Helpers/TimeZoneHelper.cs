using System;

namespace Monoplist.Helpers
{
    public static class TimeZoneHelper
    {
      
        private static readonly TimeSpan TargetOffset = TimeSpan.FromHours(5);

        /// Преобразует UTC-время в локальное время с заданным смещением.
        public static DateTime ConvertToLocal(DateTime utcDateTime)
        {
            // Если время уже имеет тип Utc, просто добавляем смещение.
            // Если Kind не указан (Unspecified), считаем, что это тоже UTC.
            if (utcDateTime.Kind == DateTimeKind.Utc || utcDateTime.Kind == DateTimeKind.Unspecified)
                return utcDateTime + TargetOffset;

            // Если время уже локальное (например, Kind = Local) – преобразуем через ToUniversalTime (на случай ошибки)
            return utcDateTime.ToUniversalTime() + TargetOffset;
        }

        /// <summary>
        /// Форматирует дату/время в строку с учётом часового пояса.
        /// </summary>
        /// <param name="dateTime">Дата/время (ожидается в UTC)</param>
        /// <param name="format">Формат строки (по умолчанию "dd.MM.yyyy HH:mm")</param>
        /// <returns>Строка с датой в локальном времени, или "-", если дата null</returns>
        public static string Format(DateTime? dateTime, string format = "dd.MM.yyyy HH:mm")
        {
            if (dateTime == null) return "-";
            var local = ConvertToLocal(dateTime.Value);
            return local.ToString(format);
        }
    }
}