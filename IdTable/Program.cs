// Program.cs
using IdTable;
using System;
using System.Collections.Generic;
using System.Diagnostics;

class Program
{
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
                Type = "int",
                ScopeLevel = 0
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
            Console.WriteLine($"  '{id}': {(result == null ? "не найден" : "ОШИБКА - найден!")}");
        }

        // Удаление
        Console.WriteLine("\nУдаление нескольких элементов...");
        var toDelete = new[] { "alpha", "gamma", "epsilon" };
        foreach (var id in toDelete)
        {
            bool deleted = table.Delete(id);
            Console.WriteLine($"  '{id}': {(deleted ? "удалён" : "не найден")}");
        }
        Console.WriteLine($"Осталось элементов: {table.Count}");

        // Проверка, что удалённые не находятся
        Console.WriteLine("\nПроверка удалённых элементов...");
        foreach (var id in toDelete)
        {
            var result = table.Search(id);
            Console.WriteLine($"  '{id}': {(result == null ? "не найден (верно)" : "ОШИБКА - всё ещё в таблице!")}");
        }

        sw.Stop();
        Console.WriteLine($"\nВремя выполнения: {sw.ElapsedMilliseconds} мс");

        table.PrintStatistics();
    }

    static void PerformanceComparison()
    {
        Console.WriteLine("--- СРАВНЕНИЕ ПРОИЗВОДИТЕЛЬНОСТИ ---\n");

        var sizes = new[] { 1000, 5000, 10000, 20000, 100000 };

        foreach (var size in sizes)
        {
            Console.WriteLine($"\n=== Размер данных: {size} ===");

            // Генерируем данные
            var identifiers = new List<string>();
            for (int i = 0; i < size; i++)
            {
                identifiers.Add($"id_{i:D6}");
            }

            // Тест хеш-таблицы с PRNG
            var hashTablePRNG = new HashTableWithPRNG(64, 42);
            var sw1 = Stopwatch.StartNew();

            foreach (var id in identifiers)
            {
                hashTablePRNG.Insert(new Entry(id));
            }
            foreach (var id in identifiers)
            {
                hashTablePRNG.Search(id);
            }

            sw1.Stop();

            // Тест хеш-таблицы с цепочками
            var hashTableChaining = new HashTableWithChaining(64);
            var sw2 = Stopwatch.StartNew();

            foreach (var id in identifiers)
            {
                hashTableChaining.Insert(new Entry(id));
            }
            foreach (var id in identifiers)
            {
                hashTableChaining.Search(id);
            }

            sw2.Stop();

            // Тест списка
            var listTable = new SimpleListTable();
            var sw3 = Stopwatch.StartNew();

            foreach (var id in identifiers)
            {
                listTable.Insert(new Entry(id));
            }
            foreach (var id in identifiers)
            {
                listTable.Search(id);
            }

            sw3.Stop();

            Console.WriteLine($"Хеш-таблица с PRNG:     {sw1.ElapsedMilliseconds} мс");
            Console.WriteLine($"Хеш-таблица с цепочками: {sw2.ElapsedMilliseconds} мс");
            Console.WriteLine($"Простой список:         {sw3.ElapsedMilliseconds} мс");
            Console.WriteLine($"Ускорение PRNG vs список: {(double)sw3.ElapsedMilliseconds / sw1.ElapsedMilliseconds:F2}x");
            Console.WriteLine($"Ускорение цепочки vs список: {(double)sw3.ElapsedMilliseconds / sw2.ElapsedMilliseconds:F2}x");
        }
    }

    static IIdentifierTable SelectTable()
    {
        Console.WriteLine("\n=== ВЫБОР ТИПА ТАБЛИЦЫ ===");
        Console.WriteLine("1. Хеш-таблица с открытой адресацией (PRNG)");
        Console.WriteLine("2. Хеш-таблица с цепочками");
        Console.WriteLine("3. Простой список");
        Console.Write("\nВыберите тип таблицы (1-3): ");

        string choice = Console.ReadLine() ?? "1";

        switch (choice)
        {
            case "1":
                Console.WriteLine("Выбрана: Хеш-таблица с PRNG");
                return new HashTableWithPRNG(128, 42);
            case "2":
                Console.WriteLine("Выбрана: Хеш-таблица с цепочками");
                return new HashTableWithChaining(128);
            case "3":
                Console.WriteLine("Выбран: Простой список");
                return new SimpleListTable();
            default:
                Console.WriteLine("Некорректный выбор. Используется хеш-таблица с PRNG по умолчанию.");
                return new HashTableWithPRNG(128, 42);
        }
    }

    static void InteractiveMode()
    {
        IIdentifierTable table = SelectTable();

        Console.WriteLine("\n=== ИНТЕРАКТИВНЫЙ РЕЖИМ ===");
        Console.WriteLine("Команды:");
        Console.WriteLine("  add <имя> [тип] [вид] [уровень] - добавить идентификатор");
        Console.WriteLine("  get <имя>                        - получить идентификатор");
        Console.WriteLine("  del <имя>                        - удалить идентификатор");
        Console.WriteLine("  stat                             - показать статистику");
        Console.WriteLine("  exit                             - выход\n");

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
                    int scope = parts.Length > 4 && int.TryParse(parts[4], out int s) ? s : 0;

                    var entry = new Entry(name)
                    {
                        Type = type,
                        Kind = kind,
                        ScopeLevel = scope
                    };

                    if (table.Insert(entry))
                    {
                        Console.WriteLine($"✓ Добавлен: {name} ({kind}, {type}, уровень {scope})");
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
                        Console.WriteLine($"  Тип:    {result.Type}");
                        Console.WriteLine($"  Вид:    {result.Kind}");
                        Console.WriteLine($"  Уровень: {result.ScopeLevel}");
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

        // Тестовые данные
        var testIdentifiers = new List<string>
        {
            "alpha", "beta", "gamma", "delta", "epsilon",
            "zeta", "eta", "theta", "iota", "kappa",
            "lambda", "mu", "nu", "xi", "omicron",
            "pi", "rho", "sigma", "tau", "upsilon",
            "phi", "chi", "psi", "omega", "count",
            "sum", "average", "total", "index", "value"
        };

        Console.WriteLine($"Тестируем с {testIdentifiers.Count} идентификаторами\n");

        Console.WriteLine("--- ТЕСТ 1: Хеш-таблица с рехешированием (PRNG) ---");
        TestTable(new HashTableWithPRNG(32, 42), testIdentifiers);
        Console.WriteLine("\n" + new string('=', 60) + "\n");

        Console.WriteLine("--- ТЕСТ 2: Хеш-таблица с цепочками ---");
        TestTable(new HashTableWithChaining(32), testIdentifiers);
        Console.WriteLine("\n" + new string('=', 60) + "\n");

        Console.WriteLine("--- ТЕСТ 3: Простой список ---");
        TestTable(new SimpleListTable(), testIdentifiers);
        Console.WriteLine("\n" + new string('=', 60) + "\n");

        PerformanceComparison();

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        // Интерактивный режим
        InteractiveMode();
    }
}
