using System;

namespace MemOptima
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "MemOptima - Комплекс оптимизации ОЗУ и SSD | Алиев Марат (РУП-8)";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================================");
            Console.WriteLine("            MEMOPTIMA: SYSTEM RESOURCE OPTIMIZATION              ");
            Console.WriteLine("     Проект по исследованию подорожания памяти (SSD & RAM)        ");
            Console.WriteLine("     Автор: Алиев Марат, РУП-8 | Науч. рук.: Худошина М.А.       ");
            Console.WriteLine("==================================================================");
            Console.ResetColor();

            while (true)
            {
                var mem = SystemOptimizer.GetMemoryStatus();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[СТАТУС ПАМЯТИ]");
                Console.ResetColor();
                Console.WriteLine($"Всего ОЗУ:        {mem.TotalGB} ГБ");
                Console.WriteLine($"Используется:     {mem.UsedGB} ГБ ({mem.LoadPercentage}%)");
                Console.WriteLine($"Свободно:         {mem.FreeGB} ГБ");

                Console.WriteLine();
                Console.WriteLine("Выберите действие:");
                Console.WriteLine("1. Глубокая оптимизация RAM (EmptyWorkingSet)");
                Console.WriteLine("2. Очистка временных кэшей (Discord, Telegram, Chrome, Temp)");
                Console.WriteLine("3. Сжать директорию на SSD (NTFS CompactOS XPRESS8K)");
                Console.WriteLine("4. Комплексная оптимизация (RAM + Очистка кэша)");
                Console.WriteLine("0. Выход");
                Console.Write("\nВаш выбор: ");

                string choice = Console.ReadLine();
                if (choice == "0") break;

                switch (choice)
                {
                    case "1":
                        Console.WriteLine("\n[Оптимизация] Сброс рабочих наборов процессов...");
                        int count = SystemOptimizer.OptimizeRAM(out double freedMB);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[Успех] Обработано процессов: {count}. Высвобождено ОЗУ: {Math.Round(freedMB, 1)} МБ!");
                        Console.ResetColor();
                        break;

                    case "2":
                        Console.WriteLine("\n[Очистка] Удаление временных кэшей...");
                        long diskFreed = SystemOptimizer.PurgeApplicationCaches();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[Успех] Освобождено на накопителе: ~{diskFreed} МБ дискового пространства!");
                        Console.ResetColor();
                        break;

                    case "3":
                        Console.Write("\nВведите полный путь к папке для сжатия (например, C:\\Games\\MyGame): ");
                        string folder = Console.ReadLine();
                        Console.WriteLine("[Сжатие] Применение алгоритма XPRESS8K... Это может занять некоторое время.");
                        bool ok = SystemOptimizer.CompressFolder(folder, out string log);
                        if (ok)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine("[Успех] Папка успешно сжата без потери скорости доступа!");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[Ошибка] {log}");
                        }
                        Console.ResetColor();
                        break;

                    case "4":
                        Console.WriteLine("\n[Комплекс] Выполняется полная оптимизация ресурсов...");
                        int pCount = SystemOptimizer.OptimizeRAM(out double fMB);
                        long cFreed = SystemOptimizer.PurgeApplicationCaches();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"[Успех] Обработано {pCount} процессов.");
                        Console.WriteLine($"Высвобождено ОЗУ: {Math.Round(fMB, 1)} МБ.");
                        Console.WriteLine($"Освобождено на SSD: {cFreed} МБ.");
                        Console.ResetColor();
                        break;

                    default:
                        Console.WriteLine("Неверный пункт меню.");
                        break;
                }

                Console.WriteLine("\nНажмите Enter для продолжения...");
                Console.ReadLine();
            }
        }
    }
}
