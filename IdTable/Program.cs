// Program.cs

using IdTable;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

class Program
{
    static void InsertDefaultIdentifiers(IIdentifierTable table)
    {
        var defaultIdentifiers = new (string name, string kind, string type)[]
        {
            ("a", "const", "void"),
            ("b", "const", "int"),
            ("c", "var", "int"),
            ("d", "const", "void"),
            ("e", "const", "int"),
            ("f", "const", "int"),
            ("g", "var", "float"),
            ("h", "const", "void"),
            ("i", "const", "void"),
            ("j", "const", "int"),
            ("k", "var", "double"),
            ("l", "var", "char"),
            ("m", "var", "long"),
            ("n", "var", "pointer"),
            ("o", "const", "pointer"),
            ("p", "const", "void"),
            ("q", "var", "short"),
            ("r", "var", "unsigned int"),
            ("s", "var", "bool"),
            ("t", "var", "pointer")
        };


        foreach (var (name, kind, type) in defaultIdentifiers)
        {
            var entry = new Entry(name)
            {
                Kind = kind,
                Type = type
            };
            table.Insert(entry);
        }
    }

    class PerformanceTrialData
    {
        public int Size { get; set; }
        public List<long> CustomInsertTimes { get; set; } = new();
        public List<long> CustomSearchTimes { get; set; } = new();
        public List<long> CustomDeleteTimes { get; set; } = new();
        public List<long> SystemInsertTimes { get; set; } = new();
        public List<long> SystemSearchTimes { get; set; } = new();
        public List<long> SystemDeleteTimes { get; set; } = new();
        public List<int> CustomCollisions { get; set; } = new();
        public List<int> SystemCollisions { get; set; } = new();

        // Средние значения
        public long CustomInsertAvg => (long)CustomInsertTimes.Average();
        public long CustomSearchAvg => (long)CustomSearchTimes.Average();
        public long CustomDeleteAvg => (long)CustomDeleteTimes.Average();
        public long SystemInsertAvg => (long)SystemInsertTimes.Average();
        public long SystemSearchAvg => (long)SystemSearchTimes.Average();
        public long SystemDeleteAvg => (long)SystemDeleteTimes.Average();
        public double CustomCollisionsAvg => CustomCollisions.Average();
        public double SystemCollisionsAvg => SystemCollisions.Average();
    }

    static string GeneratePythonPlotScript(List<PerformanceTrialData> data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import matplotlib.pyplot as plt");
        sb.AppendLine("import numpy as np");
        sb.AppendLine();

        // Подготовка данных
        sb.AppendLine("# Размеры данных");
        sb.Append("sizes = [");
        sb.Append(string.Join(", ", data.Select(d => d.Size)));
        sb.AppendLine("]");

        sb.AppendLine();
        sb.AppendLine("# === ПРОИЗВОДИТЕЛЬНОСТЬ ===");
        sb.AppendLine("# Кастомная PRNG");
        sb.Append("custom_insert = [");
        sb.Append(string.Join(", ", data.Select(d => d.CustomInsertAvg)));
        sb.AppendLine("]");

        sb.Append("custom_search = [");
        sb.Append(string.Join(", ", data.Select(d => d.CustomSearchAvg)));
        sb.AppendLine("]");

        sb.Append("custom_delete = [");
        sb.Append(string.Join(", ", data.Select(d => d.CustomDeleteAvg)));
        sb.AppendLine("]");

        sb.AppendLine();
        sb.AppendLine("# System.Random");
        sb.Append("system_insert = [");
        sb.Append(string.Join(", ", data.Select(d => d.SystemInsertAvg)));
        sb.AppendLine("]");

        sb.Append("system_search = [");
        sb.Append(string.Join(", ", data.Select(d => d.SystemSearchAvg)));
        sb.AppendLine("]");

        sb.Append("system_delete = [");
        sb.Append(string.Join(", ", data.Select(d => d.SystemDeleteAvg)));
        sb.AppendLine("]");

        sb.AppendLine();
        sb.AppendLine("# === СТАТИСТИКА КОЛЛИЗИЙ ===");
        sb.Append("custom_collisions = [");
        sb.Append(string.Join(", ", data.Select(d => d.CustomCollisionsAvg.ToString("0.0").Replace(",", "."))));
        sb.AppendLine("]");

        sb.Append("system_collisions = [");
        sb.Append(string.Join(", ", data.Select(d => d.SystemCollisionsAvg.ToString("0.0").Replace(",", "."))));
        sb.AppendLine("]");

        sb.AppendLine();
        sb.AppendLine("# Создаём фигуру с подграфиками (2x3)");
        sb.AppendLine("fig, axes = plt.subplots(2, 3, figsize=(20, 10))");
        sb.AppendLine("fig.suptitle('Сравнение PRNG: Производительность и Статистика Коллизий', fontsize=18, fontweight='bold')");
        sb.AppendLine();

        // Первая строка - производительность
        sb.AppendLine("# === ПРОИЗВОДИТЕЛЬНОСТЬ ===");
        sb.AppendLine("# График 1: Вставка");
        sb.AppendLine("axes[0, 0].plot(sizes, custom_insert, 'o-', label='Кастомная PRNG', linewidth=2.5, markersize=8, color='#2E86AB')");
        sb.AppendLine("axes[0, 0].plot(sizes, system_insert, 's-', label='System.Random', linewidth=2.5, markersize=8, color='#A23B72')");
        sb.AppendLine("axes[0, 0].set_xlabel('Количество элементов', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[0, 0].set_ylabel('Время (мс)', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[0, 0].set_title('Операция вставки', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[0, 0].legend(fontsize=10, loc='upper left')");
        sb.AppendLine("axes[0, 0].grid(True, alpha=0.3)");
        sb.AppendLine("axes[0, 0].set_xscale('log')");
        sb.AppendLine();

        // График 2: Поиск
        sb.AppendLine("# График 2: Поиск");
        sb.AppendLine("axes[0, 1].plot(sizes, custom_search, 'o-', label='Кастомная PRNG', linewidth=2.5, markersize=8, color='#2E86AB')");
        sb.AppendLine("axes[0, 1].plot(sizes, system_search, 's-', label='System.Random', linewidth=2.5, markersize=8, color='#A23B72')");
        sb.AppendLine("axes[0, 1].set_xlabel('Количество элементов', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[0, 1].set_ylabel('Время (мс)', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[0, 1].set_title('Операция поиска', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[0, 1].legend(fontsize=10, loc='upper left')");
        sb.AppendLine("axes[0, 1].grid(True, alpha=0.3)");
        sb.AppendLine("axes[0, 1].set_xscale('log')");
        sb.AppendLine();

        // График 3: Удаление
        sb.AppendLine("# График 3: Удаление");
        sb.AppendLine("axes[0, 2].plot(sizes, custom_delete, 'o-', label='Кастомная PRNG', linewidth=2.5, markersize=8, color='#2E86AB')");
        sb.AppendLine("axes[0, 2].plot(sizes, system_delete, 's-', label='System.Random', linewidth=2.5, markersize=8, color='#A23B72')");
        sb.AppendLine("axes[0, 2].set_xlabel('Количество элементов', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[0, 2].set_ylabel('Время (мс)', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[0, 2].set_title('Операция удаления', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[0, 2].legend(fontsize=10, loc='upper left')");
        sb.AppendLine("axes[0, 2].grid(True, alpha=0.3)");
        sb.AppendLine("axes[0, 2].set_xscale('log')");
        sb.AppendLine();

        // Вторая строка - коллизии
        sb.AppendLine("# === СТАТИСТИКА КОЛЛИЗИЙ ===");
        sb.AppendLine("# График 4: Коллизии (линейный график)");
        sb.AppendLine("axes[1, 0].plot(sizes, custom_collisions, 'o-', label='Кастомная PRNG', linewidth=2.5, markersize=8, color='#2E86AB')");
        sb.AppendLine("axes[1, 0].plot(sizes, system_collisions, 's-', label='System.Random', linewidth=2.5, markersize=8, color='#A23B72')");
        sb.AppendLine("axes[1, 0].set_xlabel('Количество элементов', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[1, 0].set_ylabel('Среднее число коллизий', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[1, 0].set_title('Коллизии (линейная шкала)', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[1, 0].legend(fontsize=10, loc='upper left')");
        sb.AppendLine("axes[1, 0].grid(True, alpha=0.3)");
        sb.AppendLine();

        // График 5: Коллизии (логарифмическая)
        sb.AppendLine("# График 5: Коллизии (логарифмическая шкала)");
        sb.AppendLine("axes[1, 1].semilogy(sizes, custom_collisions, 'o-', label='Кастомная PRNG', linewidth=2.5, markersize=8, color='#2E86AB')");
        sb.AppendLine("axes[1, 1].semilogy(sizes, system_collisions, 's-', label='System.Random', linewidth=2.5, markersize=8, color='#A23B72')");
        sb.AppendLine("axes[1, 1].set_xlabel('Количество элементов', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[1, 1].set_ylabel('Среднее число коллизий (log)', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[1, 1].set_title('Коллизии (логарифмическая шкала)', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[1, 1].legend(fontsize=10, loc='upper left')");
        sb.AppendLine("axes[1, 1].grid(True, alpha=0.3)");
        sb.AppendLine("axes[1, 1].set_xscale('log')");
        sb.AppendLine();

        // График 6: Сравнение коллизий (столбчатая диаграмма)
        sb.AppendLine("# График 6: Разница в коллизиях");
        sb.AppendLine("collisions_diff = [c - s for c, s in zip(custom_collisions, system_collisions)]");
        sb.AppendLine("colors = ['#2E86AB' if x < 0 else '#A23B72' for x in collisions_diff]");
        sb.AppendLine("axes[1, 2].bar(range(len(sizes)), collisions_diff, color=colors, alpha=0.7, edgecolor='black')");
        sb.AppendLine("axes[1, 2].axhline(y=0, color='black', linestyle='-', linewidth=0.8)");
        sb.AppendLine("axes[1, 2].set_xlabel('Индекс размера данных', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[1, 2].set_ylabel('Разница коллизий', fontsize=11, fontweight='bold')");
        sb.AppendLine("axes[1, 2].set_title('Разница коллизий (Custom - System)', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[1, 2].grid(True, alpha=0.3, axis='y')");
        sb.AppendLine("axes[1, 2].set_xticks(range(len(sizes)))");
        sb.AppendLine("axes[1, 2].set_xticklabels(sizes)");
        sb.AppendLine();

        sb.AppendLine("plt.tight_layout()");
        sb.AppendLine("plt.savefig('performance_comparison_detailed.png', dpi=300, bbox_inches='tight')");
        sb.AppendLine("print('Графики сохранены в файл: performance_comparison_detailed.png')");
        sb.AppendLine("plt.show()");

        return sb.ToString();
    }

    class TableStatistics
    {
        public long TotalProbes { get; set; }
        public int Collisions { get; set; }
    }

    static TableStatistics ExtractTableStats(HashTableWithPRNG table)
    {
        var stats = new TableStatistics();
        // Используем reflection для доступа к приватным полям
        var type = table.GetType();
        var totalProbesField = type.GetField("_totalProbes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var collisionsField = type.GetField("_collisions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (totalProbesField != null)
            stats.TotalProbes = (long)totalProbesField.GetValue(table);
        if (collisionsField != null)
            stats.Collisions = (int)collisionsField.GetValue(table);

        return stats;
    }

    static void PerformanceComparison()
    {
        const int TRIALS = 10;
        Console.WriteLine($"--- СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ (с повторением {TRIALS} раз) ---\n");
        var sizes = new[] { 1000, 5000, 10000, 50000, 68023, 75490, 91443, 103595, 110482, 133590, 156868, 185437, 212942, 265284, 289052, 342674, 399854, 617971, 811092, 1000000 };


        Console.WriteLine("=== СРАВНЕНИЕ: Стандартная ГПСЧ vs Кастомная PRNG ===\n");

        var performanceData = new List<PerformanceTrialData>();

        foreach (var size in sizes)
        {
            Console.WriteLine($"\n--- Размер данных: {size} (повторений: {TRIALS}) ---");

            var trialData = new PerformanceTrialData { Size = size };

            for (int trial = 0; trial < TRIALS; trial++)
            {
                if ((trial + 1) % 20 == 0)
                    Console.Write($"\r  Прогресс: {trial + 1}/{TRIALS}");

                // Генерируем данные
                var identifiers = new List<string>();
                for (int i = 0; i < size; i++)
                {
                    identifiers.Add($"id_{i:D6}_{trial}");
                }

                // ===== КАСТОМНАЯ PRNG =====
                var hashTableCustom = new HashTableWithPRNG(128, 42 + trial, new PRNG(42 + trial));

                // Вставка
                var sw = Stopwatch.StartNew();
                foreach (var id in identifiers)
                {
                    hashTableCustom.Insert(new Entry(id) { Kind = "var", Type = "int" });
                }
                sw.Stop();
                trialData.CustomInsertTimes.Add(sw.ElapsedMilliseconds);

                // Поиск
                sw = Stopwatch.StartNew();
                foreach (var id in identifiers)
                {
                    hashTableCustom.Search(id);
                }
                sw.Stop();
                trialData.CustomSearchTimes.Add(sw.ElapsedMilliseconds);

                // Удаление
                sw = Stopwatch.StartNew();
                for (int i = 0; i < size / 2; i++)
                {
                    hashTableCustom.Delete($"id_{i:D6}_{trial}");
                }
                sw.Stop();
                trialData.CustomDeleteTimes.Add(sw.ElapsedMilliseconds);

                var customStats = ExtractTableStats(hashTableCustom);
                trialData.CustomCollisions.Add(customStats.Collisions);

                // ===== SYSTEM.RANDOM =====
                var hashTableSystem = new HashTableWithPRNG(128, 42 + trial, new SystemRandomAdapter(42 + trial));

                // Вставка
                sw = Stopwatch.StartNew();
                foreach (var id in identifiers)
                {
                    hashTableSystem.Insert(new Entry(id) { Kind = "var", Type = "int" });
                }
                sw.Stop();
                trialData.SystemInsertTimes.Add(sw.ElapsedMilliseconds);

                // Поиск
                sw = Stopwatch.StartNew();
                foreach (var id in identifiers)
                {
                    hashTableSystem.Search(id);
                }
                sw.Stop();
                trialData.SystemSearchTimes.Add(sw.ElapsedMilliseconds);

                // Удаление
                sw = Stopwatch.StartNew();
                for (int i = 0; i < size / 2; i++)
                {
                    hashTableSystem.Delete($"id_{i:D6}_{trial}");
                }
                sw.Stop();
                trialData.SystemDeleteTimes.Add(sw.ElapsedMilliseconds);

                var systemStats = ExtractTableStats(hashTableSystem);
                trialData.SystemCollisions.Add(systemStats.Collisions);
            }

            Console.WriteLine($"\r  Прогресс: {TRIALS}/{TRIALS}     ");
            Console.WriteLine($"\n  Результаты ({size} элементов):");
            Console.WriteLine($"    Вставка:  Custom: {trialData.CustomInsertAvg} мс | System: {trialData.SystemInsertAvg} мс | Ratio: {(double)trialData.CustomInsertAvg / trialData.SystemInsertAvg:F2}x");
            Console.WriteLine($"    Поиск:    Custom: {trialData.CustomSearchAvg} мс | System: {trialData.SystemSearchAvg} мс | Ratio: {(double)trialData.CustomSearchAvg / trialData.SystemSearchAvg:F2}x");
            Console.WriteLine($"    Удаление: Custom: {trialData.CustomDeleteAvg} мс | System: {trialData.SystemDeleteAvg} мс | Ratio: {(double)trialData.CustomDeleteAvg / trialData.SystemDeleteAvg:F2}x");
            Console.WriteLine($"    Коллизии: Custom: {trialData.CustomCollisionsAvg:F1} | System: {trialData.SystemCollisionsAvg:F1} | Разница: {trialData.CustomCollisionsAvg - trialData.SystemCollisionsAvg:F1}");

            performanceData.Add(trialData);
        }

        // Генерируем Python скрипт
        string pythonScript = GeneratePythonPlotScript(performanceData);
        string scriptPath = "plot_performance.py";
        System.IO.File.WriteAllText(scriptPath, pythonScript);

        Console.WriteLine($"\n\n✓ Python скрипт сохранён в файл: {scriptPath}");
        Console.WriteLine("\nДля построения графика выполните в консоли:");
        Console.WriteLine($"  python {scriptPath}");
        Console.WriteLine("\nИли скопируйте и выполните следующий Python код:");
        Console.WriteLine("\n" + new string('=', 100));
        Console.WriteLine(pythonScript);
        Console.WriteLine(new string('=', 100));
    }

    static IIdentifierTable SelectTable()
    {
        Console.WriteLine("\n=== ВЫБОР ТИПА ТАБЛИЦЫ ===");
        Console.WriteLine("1. Хеш-таблица с открытой адресацией (PRNG)");
        Console.WriteLine("2. Хеш-таблица с открытой адресацией (System.Random)");
        Console.WriteLine("3. Хеш-таблица с цепочками");
        Console.WriteLine("4. Простой список");
        Console.Write("\nВыберите тип таблицы (1-4): ");
        string choice = Console.ReadLine() ?? "1";

        switch (choice)
        {
            case "1":
                Console.WriteLine("Выбрана: Хеш-таблица с PRNG");
                return new HashTableWithPRNG(128, 42, new PRNG(42));
            case "2":
                Console.WriteLine("Выбрана: Хеш-таблица с System.Random");
                return new HashTableWithPRNG(128, 42, new SystemRandomAdapter(42));
            case "3":
                Console.WriteLine("Выбрана: Хеш-таблица с цепочками");
                return new HashTableWithChaining(128);
            case "4":
                Console.WriteLine("Выбран: Простой список");
                return new SimpleListTable();
            default:
                Console.WriteLine("Некорректный выбор. Используется хеш-таблица с PRNG по умолчанию.");
                return new HashTableWithPRNG(128, 42, new PRNG(42));
        }
    }

    static void InteractiveMode()
    {
        IIdentifierTable table = SelectTable();
        InsertDefaultIdentifiers(table);
        Console.WriteLine("\n=== ИНТЕРАКТИВНЫЙ РЕЖИМ ===");
        Console.WriteLine("Команды:");
        Console.WriteLine(" add <имя> [тип] [вид] - добавить идентификатор");
        Console.WriteLine(" get <имя> - получить идентификатор");
        Console.WriteLine(" del <имя> - удалить идентификатор");
        Console.WriteLine(" delpos <позиция> - удалить по позиции");
        Console.WriteLine(" stat - показать статистику");
        Console.WriteLine(" exit - выход\n");

        while (true)
        {
            Console.Write("> ");
            string input = Console.ReadLine()?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(input))
                continue;

            string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLower();

            switch (command)
            {
                case "add":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Ошибка: укажите имя идентификатора");
                        break;
                    }

                    string name = parts[1];
                    string type = parts.Length > 2 ? parts[2] : "int";
                    string kind = parts.Length > 3 ? parts[3] : "var";

                    var entry = new Entry(name)
                    {
                        Type = type,
                        Kind = kind
                    };

                    if (table.Insert(entry))
                    {
                        Console.WriteLine($"✓ Добавлен: {name} ({kind}, {type}), адрес: {entry.Address}");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Ошибка при добавлении '{name}'");
                    }
                    break;

                case "get":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Ошибка: укажите имя идентификатора");
                        break;
                    }

                    string searchName = parts[1];
                    var result = table.Search(searchName);
                    if (result != null)
                    {
                        Console.WriteLine($"✓ Найден: {result.Name}");
                        Console.WriteLine($"  Тип: {result.Type}");
                        Console.WriteLine($"  Вид: {result.Kind}");
                        Console.WriteLine($"  Адрес: {result.Address}");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Идентификатор '{searchName}' не найден");
                    }
                    break;

                case "del":
                    if (parts.Length < 2)
                    {
                        Console.WriteLine("Ошибка: укажите имя идентификатора");
                        break;
                    }

                    string deleteName = parts[1];
                    if (table.Delete(deleteName))
                    {
                        Console.WriteLine($"✓ Удалён: {deleteName}");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Идентификатор '{deleteName}' не найден");
                    }
                    break;

                case "delpos":
                    if (parts.Length < 2 || !int.TryParse(parts[1], out int pos))
                    {
                        Console.WriteLine("Ошибка: укажите позицию (число)");
                        break;
                    }

                    if (table.DeleteByPosition(pos))
                    {
                        Console.WriteLine($"✓ Элемент на позиции {pos} удалён");
                    }
                    else
                    {
                        Console.WriteLine($"✗ Ошибка при удалении на позиции {pos}");
                    }
                    break;

                case "stat":
                    table.PrintStatistics();
                    break;

                case "exit":
                    Console.WriteLine("Выход из интерактивного режима.");
                    return;

                default:
                    Console.WriteLine($"Неизвестная команда: '{command}'");
                    break;
            }
        }
    }

    static void Main(string[] args)
    {
        PRNG rng = new PRNG();
        rng.DumpOneMillion("rng_samples.txt");
        Console.WriteLine("=== Лабораторная работа: Таблица идентификаторов ===\n");

        // Сравнение производительности с повторениями
        PerformanceComparison();

        // Затем переходим в интерактивный режим
        Console.WriteLine("\n\nНажмите Enter для входа в интерактивный режим...");
        Console.ReadLine();

        InteractiveMode();
    }
}
