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
            ("printf", "func", "void"),
            ("scanf", "func", "int"),
            ("a", "var", "int"),
            ("free", "func", "void"),
            ("strlen", "func", "int"),
            ("strcmp", "func", "int"),
            ("i", "var", "float"),
            ("strcat", "func", "void"),
            ("main", "func", "void"),
            ("test", "func", "int"),
            ("x", "var", "double"),
            ("y", "var", "char"),
            ("z", "var", "long"),
            ("arr", "var", "pointer"),
            ("malloc", "func", "pointer"),
            ("memset", "func", "void"),
            ("j", "var", "short"),
            ("k", "var", "unsigned int"),
            ("tmp", "var", "bool"),
            ("buffer", "var", "pointer")
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

    static void TestTable(IIdentifierTable table, List<string> identifiers)
    {
        var sw = Stopwatch.StartNew();

        // Вставка
        Console.WriteLine("Вставка элементов...");
        foreach (var id in identifiers)
        {
            var entry = new Entry(id)
            {
                Kind = "var",
                Type = "int"
            };
            table.Insert(entry);
        }

        Console.WriteLine($"Вставлено {table.Count} элементов");

        // Поиск существующих
        Console.WriteLine("\nПоиск существующих элементов...");
        int found = 0;
        foreach (var id in identifiers)
        {
            var result = table.Search(id);
            if (result != null)
                found++;
        }
        Console.WriteLine($"Найдено: {found}/{identifiers.Count}");

        // Поиск несуществующих
        Console.WriteLine("\nПоиск несуществующих элементов...");
        var notExisting = new[] { "notfound1", "notfound2", "notfound3" };
        foreach (var id in notExisting)
        {
            var result = table.Search(id);
            Console.WriteLine($" '{id}': {(result == null ? "не найден" : "ОШИБКА - найден!")}");
        }

        // Удаление
        Console.WriteLine("\nУдаление нескольких элементов...");
        var toDelete = new[] { "alpha", "gamma", "epsilon" };
        foreach (var id in toDelete)
        {
            bool deleted = table.Delete(id);
            Console.WriteLine($" '{id}': {(deleted ? "удалён" : "не найден")}");
        }

        Console.WriteLine($"Осталось элементов: {table.Count}");

        // Проверка, что удалённые не находятся
        Console.WriteLine("\nПроверка удалённых элементов...");
        foreach (var id in toDelete)
        {
            var result = table.Search(id);
            Console.WriteLine($" '{id}': {(result == null ? "не найден (верно)" : "ОШИБКА - всё ещё в таблице!")}");
        }

        sw.Stop();
        Console.WriteLine($"\nВремя выполнения: {sw.ElapsedMilliseconds} мс");
        table.PrintStatistics();
    }

    static PRNGQualityStats AnalyzePRNG(PRNGBase prng, string generatorName, int sampleSize = 100000, int buckets = 10)
    {
        var samples = new double[sampleSize];

        // Генерируем выборки
        for (int i = 0; i < sampleSize; i++)
        {
            samples[i] = prng.NextDouble();
        }

        // Базовая статистика
        double min = samples.Min();
        double max = samples.Max();
        double mean = samples.Average();

        // Медиана
        Array.Sort(samples);
        double median = sampleSize % 2 == 0
            ? (samples[sampleSize / 2 - 1] + samples[sampleSize / 2]) / 2
            : samples[sampleSize / 2];

        // Стандартное отклонение и дисперсия
        double variance = samples.Sum(x => (x - mean) * (x - mean)) / sampleSize;
        double stdDev = Math.Sqrt(variance);

        // Асимметрия (skewness) и эксцесс (kurtosis)
        double m3 = samples.Sum(x => Math.Pow(x - mean, 3)) / sampleSize;
        double m4 = samples.Sum(x => Math.Pow(x - mean, 4)) / sampleSize;
        double skewness = m3 / Math.Pow(stdDev, 3);
        double kurtosis = (m4 / (stdDev * stdDev * stdDev * stdDev)) - 3; // избыточный эксцесс

        // Chi-square тест на равномерность
        var bucketCounts = new int[buckets];
        foreach (var sample in samples)
        {
            int bucketIndex = (int)(sample * buckets);
            if (bucketIndex >= buckets) bucketIndex = buckets - 1;
            bucketCounts[bucketIndex]++;
        }

        double expectedPerBucket = (double)sampleSize / buckets;
        double chiSquare = 0;
        for (int i = 0; i < buckets; i++)
        {
            double diff = bucketCounts[i] - expectedPerBucket;
            chiSquare += (diff * diff) / expectedPerBucket;
        }

        // Chi-square p-value (приближение)
        // Для df=9, критическое значение при α=0.05 это ~16.919
        double chiSquareNormalized = Math.Min(chiSquare / 20.0, 1.0);
        double uniformityScore = (1.0 - chiSquareNormalized) * 100.0;

        // Корреляция между соседними значениями
        double correlation = 0;
        if (sampleSize > 1)
        {
            double cov = 0;
            for (int i = 0; i < sampleSize - 1; i++)
            {
                cov += (samples[i] - mean) * (samples[i + 1] - mean);
            }
            cov /= (sampleSize - 1);
            correlation = cov / variance;
        }

        // Оценка энтропии (используя распределение по бакетам)
        double entropy = 0;
        for (int i = 0; i < buckets; i++)
        {
            double p = (double)bucketCounts[i] / sampleSize;
            if (p > 0)
            {
                entropy -= p * Math.Log2(p);
            }
        }
        double entropyScore = (entropy / Math.Log2(buckets)) * 100.0; // нормализуем к максимуму log2(buckets)

        var stats = new PRNGQualityStats
        {
            GeneratorName = generatorName,
            SampleSize = sampleSize,
            Min = min,
            Max = max,
            Mean = mean,
            Median = median,
            StdDev = stdDev,
            Variance = variance,
            Skewness = skewness,
            Kurtosis = kurtosis,
            ChiSquareStatistic = chiSquare,
            UniformityScore = uniformityScore,
            CorrelationCoefficient = correlation,
            EntropyEstimate = entropyScore,
            BucketDistribution = bucketCounts.Select((count, idx) => new { idx, count })
                .ToDictionary(x => x.idx, x => x.count)
        };

        return stats;
    }

    static void PrintPRNGAnalysis(PRNGQualityStats stats)
    {
        Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                    АНАЛИЗ КАЧЕСТВА ГЕНЕРАТОРА: {stats.GeneratorName,-33} ║");
        Console.WriteLine($"╚══════════════════════════════════════════════════════════════════════════════╝");

        Console.WriteLine($"\n📊 БАЗОВАЯ СТАТИСТИКА ({stats.SampleSize} выборок):");
        Console.WriteLine($"  Минимум:              {stats.Min:F8}");
        Console.WriteLine($"  Максимум:             {stats.Max:F8}");
        Console.WriteLine($"  Среднее:              {stats.Mean:F8}");
        Console.WriteLine($"  Медиана:              {stats.Median:F8}");

        Console.WriteLine($"\n📈 РАСПРЕДЕЛЕНИЕ:");
        Console.WriteLine($"  Дисперсия:            {stats.Variance:F8}");
        Console.WriteLine($"  Стандартное отклон.:  {stats.StdDev:F8}");
        Console.WriteLine($"  Асимметрия (S):       {stats.Skewness:F6} (идеал: ~0)");
        Console.WriteLine($"  Эксцесс (K):          {stats.Kurtosis:F6} (идеал: ~0)");

        Console.WriteLine($"\n🧪 ТЕСТЫ КАЧЕСТВА:");
        Console.WriteLine($"  Chi-square статистика: {stats.ChiSquareStatistic:F2}");
        Console.WriteLine($"  Оценка равномерности: {stats.UniformityScore:F2}% ✓" +
            (stats.UniformityScore >= 80 ? " ХОРОШО" : stats.UniformityScore >= 60 ? " ПРИЕМЛЕМО" : " ПЛОХО"));
        Console.WriteLine($"  Автокорреляция:       {stats.CorrelationCoefficient:F6} (идеал: ~0)");
        Console.WriteLine($"  Энтропия:             {stats.EntropyEstimate:F2}% " +
            (stats.EntropyEstimate >= 90 ? "✓ ОТЛИЧНАЯ" : stats.EntropyEstimate >= 75 ? "✓ ХОРОШАЯ" : "⚠ СРЕДНЯЯ"));

        Console.WriteLine($"\n📊 РАСПРЕДЕЛЕНИЕ ПО БАКЕТАМ:");
        for (int i = 0; i < 10; i++)
        {
            if (stats.BucketDistribution.ContainsKey(i))
            {
                int count = stats.BucketDistribution[i];
                double percentage = (count * 100.0) / stats.SampleSize;
                string bar = new string('█', (int)(percentage / 2));
                Console.WriteLine($"  [{i}] {bar,25} {percentage:F2}% ({count})");
            }
        }
    }

    static void ComparePRNGGenerators()
    {
        Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   СРАВНЕНИЕ КАЧЕСТВА ГЕНЕРАТОРОВ ПСЧ                            ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════════════════════╝");

        int sampleSize = 100000;

        // Анализ кастомного PRNG
        var customPrng = new PRNG(42);
        var customStats = AnalyzePRNG(customPrng, "Кастомный PRNG", sampleSize);
        PrintPRNGAnalysis(customStats);

        // Анализ System.Random
        var systemRandom = new SystemRandomAdapter(42);
        var systemStats = AnalyzePRNG(systemRandom, "System.Random", sampleSize);
        PrintPRNGAnalysis(systemStats);

        // Сравнение и рекомендации
        Console.WriteLine($"\n╔════════════════════════════════════════════════════════════════════════════════╗");
        Console.WriteLine($"║                            ИТОГОВОЕ СРАВНЕНИЕ                                  ║");
        Console.WriteLine($"╚════════════════════════════════════════════════════════════════════════════════╝");

        Console.WriteLine($"\nЛучше по равномерности:");
        if (customStats.UniformityScore > systemStats.UniformityScore)
            Console.WriteLine($"  ✓ Кастомный PRNG: {customStats.UniformityScore:F2}% vs {systemStats.UniformityScore:F2}%");
        else
            Console.WriteLine($"  ✓ System.Random: {systemStats.UniformityScore:F2}% vs {customStats.UniformityScore:F2}%");

        Console.WriteLine($"\nЛучше по энтропии:");
        if (customStats.EntropyEstimate > systemStats.EntropyEstimate)
            Console.WriteLine($"  ✓ Кастомный PRNG: {customStats.EntropyEstimate:F2}% vs {systemStats.EntropyEstimate:F2}%");
        else
            Console.WriteLine($"  ✓ System.Random: {systemStats.EntropyEstimate:F2}% vs {customStats.EntropyEstimate:F2}%");

        Console.WriteLine($"\nЛучше по автокорреляции (ближе к 0):");
        double customCorrAbs = Math.Abs(customStats.CorrelationCoefficient);
        double systemCorrAbs = Math.Abs(systemStats.CorrelationCoefficient);
        if (customCorrAbs < systemCorrAbs)
            Console.WriteLine($"  ✓ Кастомный PRNG: {customStats.CorrelationCoefficient:F6} vs {systemStats.CorrelationCoefficient:F6}");
        else
            Console.WriteLine($"  ✓ System.Random: {systemStats.CorrelationCoefficient:F6} vs {customStats.CorrelationCoefficient:F6}");

        // Общая оценка
        double customScore = (customStats.UniformityScore + customStats.EntropyEstimate +
                            (100 - Math.Abs(customStats.CorrelationCoefficient) * 100)) / 3;
        double systemScore = (systemStats.UniformityScore + systemStats.EntropyEstimate +
                            (100 - Math.Abs(systemStats.CorrelationCoefficient) * 100)) / 3;

        Console.WriteLine($"\n🏆 ОБЩАЯ ОЦЕНКА:");
        Console.WriteLine($"  Кастомный PRNG:  {customScore:F2}/100");
        Console.WriteLine($"  System.Random:   {systemScore:F2}/100");

        if (customScore > systemScore)
            Console.WriteLine($"\n✓ ВЫВОД: Кастомный PRNG показывает лучший результат ({customScore - systemScore:F2} баллов)");
        else if (systemScore > customScore)
            Console.WriteLine($"\n✓ ВЫВОД: System.Random показывает лучший результат ({systemScore - customScore:F2} баллов)");
        else
            Console.WriteLine($"\n✓ ВЫВОД: Оба генератора показывают одинаковое качество");
    }

    class PerformanceData
    {
        public int Size { get; set; }
        public long CustomInsert { get; set; }
        public long CustomSearch { get; set; }
        public long CustomDelete { get; set; }
        public long SystemInsert { get; set; }
        public long SystemSearch { get; set; }
        public long SystemDelete { get; set; }
    }

    static string GeneratePythonPlotScript(List<PerformanceData> data)
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
        sb.AppendLine("# Кастомная PRNG");
        sb.Append("custom_insert = [");
        sb.Append(string.Join(", ", data.Select(d => d.CustomInsert)));
        sb.AppendLine("]");

        sb.Append("custom_search = [");
        sb.Append(string.Join(", ", data.Select(d => d.CustomSearch)));
        sb.AppendLine("]");

        sb.Append("custom_delete = [");
        sb.Append(string.Join(", ", data.Select(d => d.CustomDelete)));
        sb.AppendLine("]");

        sb.AppendLine();
        sb.AppendLine("# System.Random");
        sb.Append("system_insert = [");
        sb.Append(string.Join(", ", data.Select(d => d.SystemInsert)));
        sb.AppendLine("]");

        sb.Append("system_search = [");
        sb.Append(string.Join(", ", data.Select(d => d.SystemSearch)));
        sb.AppendLine("]");

        sb.Append("system_delete = [");
        sb.Append(string.Join(", ", data.Select(d => d.SystemDelete)));
        sb.AppendLine("]");

        sb.AppendLine();
        sb.AppendLine("# Создаём фигуру с подграфиками");
        sb.AppendLine("fig, axes = plt.subplots(1, 3, figsize=(18, 5))");
        sb.AppendLine("fig.suptitle('Сравнение производительности: Кастомная PRNG vs System.Random', fontsize=16, fontweight='bold')");
        sb.AppendLine();

        // График вставки
        sb.AppendLine("# График 1: Вставка");
        sb.AppendLine("axes[0].plot(sizes, custom_insert, 'o-', label='Кастомная PRNG', linewidth=2, markersize=8, color='#2E86AB')");
        sb.AppendLine("axes[0].plot(sizes, system_insert, 's-', label='System.Random', linewidth=2, markersize=8, color='#A23B72')");
        sb.AppendLine("axes[0].set_xlabel('Количество элементов', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[0].set_ylabel('Время (мс)', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[0].set_title('Операция вставки', fontsize=13, fontweight='bold')");
        sb.AppendLine("axes[0].legend(fontsize=11)");
        sb.AppendLine("axes[0].grid(True, alpha=0.3)");
        sb.AppendLine("axes[0].set_xscale('log')");
        sb.AppendLine();

        // График поиска
        sb.AppendLine("# График 2: Поиск");
        sb.AppendLine("axes[1].plot(sizes, custom_search, 'o-', label='Кастомная PRNG', linewidth=2, markersize=8, color='#2E86AB')");
        sb.AppendLine("axes[1].plot(sizes, system_search, 's-', label='System.Random', linewidth=2, markersize=8, color='#A23B72')");
        sb.AppendLine("axes[1].set_xlabel('Количество элементов', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[1].set_ylabel('Время (мс)', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[1].set_title('Операция поиска', fontsize=13, fontweight='bold')");
        sb.AppendLine("axes[1].legend(fontsize=11)");
        sb.AppendLine("axes[1].grid(True, alpha=0.3)");
        sb.AppendLine("axes[1].set_xscale('log')");
        sb.AppendLine();

        // График удаления
        sb.AppendLine("# График 3: Удаление");
        sb.AppendLine("axes[2].plot(sizes, custom_delete, 'o-', label='Кастомная PRNG', linewidth=2, markersize=8, color='#2E86AB')");
        sb.AppendLine("axes[2].plot(sizes, system_delete, 's-', label='System.Random', linewidth=2, markersize=8, color='#A23B72')");
        sb.AppendLine("axes[2].set_xlabel('Количество элементов', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[2].set_ylabel('Время (мс)', fontsize=12, fontweight='bold')");
        sb.AppendLine("axes[2].set_title('Операция удаления', fontsize=13, fontweight='bold')");
        sb.AppendLine("axes[2].legend(fontsize=11)");
        sb.AppendLine("axes[2].grid(True, alpha=0.3)");
        sb.AppendLine("axes[2].set_xscale('log')");
        sb.AppendLine();

        sb.AppendLine("plt.tight_layout()");
        sb.AppendLine("plt.savefig('performance_comparison.png', dpi=300, bbox_inches='tight')");
        sb.AppendLine("print('График сохранён в файл: performance_comparison.png')");
        sb.AppendLine("plt.show()");

        return sb.ToString();
    }

    static void PerformanceComparison()
    {
        Console.WriteLine("--- СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ---\n");
        var sizes = new[] { 1000, 5000, 10000, 20000, 50000, 61967, 68023, 72981, 75490, 91443, 95569, 103595, 106698, 110482, 133590, 156868, 185437, 212942, 249076, 265284, 289052, 342674, 345976, 399854, 495322, 617971, 694525, 811092, 958809, 1000000 };

        Console.WriteLine("=== СРАВНЕНИЕ: Стандартная ГПСЧ vs Кастомная PRNG ===\n");

        var performanceData = new List<PerformanceData>();

        foreach (var size in sizes)
        {
            Console.WriteLine($"\n--- Размер данных: {size} ---");

            // Генерируем данные
            var identifiers = new List<string>();
            for (int i = 0; i < size; i++)
            {
                identifiers.Add($"id_{i:D6}");
            }

            // Тест хеш-таблицы с кастомной PRNG
            Console.WriteLine("\n[Кастомная PRNG]");
            var hashTableCustom = new HashTableWithPRNG(128, 42, new PRNG(42));

            var sw1 = Stopwatch.StartNew();
            foreach (var id in identifiers)
            {
                hashTableCustom.Insert(new Entry(id) { Kind = "var", Type = "int" });
            }
            sw1.Stop();
            long customInsertTime = sw1.ElapsedMilliseconds;

            sw1 = Stopwatch.StartNew();
            foreach (var id in identifiers)
            {
                hashTableCustom.Search(id);
            }
            sw1.Stop();
            long customSearchTime = sw1.ElapsedMilliseconds;

            // Удаление половины элементов
            sw1 = Stopwatch.StartNew();
            for (int i = 0; i < size / 2; i++)
            {
                hashTableCustom.Delete($"id_{i:D6}");
            }
            sw1.Stop();
            long customDeleteTime = sw1.ElapsedMilliseconds;

            Console.WriteLine($"  Вставка:  {customInsertTime} мс");
            Console.WriteLine($"  Поиск:    {customSearchTime} мс");
            Console.WriteLine($"  Удаление: {customDeleteTime} мс");

            // Тест хеш-таблицы со стандартной ГПСЧ
            Console.WriteLine("\n[Стандартная System.Random]");
            var hashTableSystem = new HashTableWithPRNG(128, 42, new SystemRandomAdapter(42));

            sw1 = Stopwatch.StartNew();
            foreach (var id in identifiers)
            {
                hashTableSystem.Insert(new Entry(id) { Kind = "var", Type = "int" });
            }
            sw1.Stop();
            long systemInsertTime = sw1.ElapsedMilliseconds;

            sw1 = Stopwatch.StartNew();
            foreach (var id in identifiers)
            {
                hashTableSystem.Search(id);
            }
            sw1.Stop();
            long systemSearchTime = sw1.ElapsedMilliseconds;

            // Удаление половины элементов
            sw1 = Stopwatch.StartNew();
            for (int i = 0; i < size / 2; i++)
            {
                hashTableSystem.Delete($"id_{i:D6}");
            }
            sw1.Stop();
            long systemDeleteTime = sw1.ElapsedMilliseconds;

            Console.WriteLine($"  Вставка:  {systemInsertTime} мс");
            Console.WriteLine($"  Поиск:    {systemSearchTime} мс");
            Console.WriteLine($"  Удаление: {systemDeleteTime} мс");

            // Выводим результаты сравнения
            Console.WriteLine($"\n[Результаты сравнения]");
            double insertRatio = systemInsertTime > 0 ? (double)customInsertTime / systemInsertTime : 1.0;
            double searchRatio = systemSearchTime > 0 ? (double)customSearchTime / systemSearchTime : 1.0;
            double deleteRatio = systemDeleteTime > 0 ? (double)customDeleteTime / systemDeleteTime : 1.0;

            Console.WriteLine($"  Вставка:  {insertRatio:F2}x (кастомная / система)");
            Console.WriteLine($"  Поиск:    {searchRatio:F2}x (кастомная / система)");
            Console.WriteLine($"  Удаление: {deleteRatio:F2}x (кастомная / система)");

            performanceData.Add(new PerformanceData
            {
                Size = size,
                CustomInsert = customInsertTime,
                CustomSearch = customSearchTime,
                CustomDelete = customDeleteTime,
                SystemInsert = systemInsertTime,
                SystemSearch = systemSearchTime,
                SystemDelete = systemDeleteTime
            });
        }

        // Генерируем Python скрипт
        string pythonScript = GeneratePythonPlotScript(performanceData);
        string scriptPath = "plot_performance.py";
        System.IO.File.WriteAllText(scriptPath, pythonScript);

        Console.WriteLine($"\n\n✓ Python скрипт сохранён в файл: {scriptPath}");
        Console.WriteLine("\nДля построения графика выполните в консоли:");
        Console.WriteLine($"  python {scriptPath}");
        Console.WriteLine("\nИли скопируйте и выполните следующий Python код:");
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine(pythonScript);
        Console.WriteLine(new string('=', 80));
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
        Console.WriteLine("=== Лабораторная работа: Таблица идентификаторов ===\n");

        // Сравнение качества генераторов
        ComparePRNGGenerators();

        Console.WriteLine("\n\nНажмите Enter для сравнения производительности...");
        Console.ReadLine();

        // Сравнение производительности
        PerformanceComparison();

        // Затем переходим в интерактивный режим
        Console.WriteLine("\n\nНажмите Enter для входа в интерактивный режим...");
        Console.ReadLine();

        InteractiveMode();
    }
}
