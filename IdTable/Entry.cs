using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace IdTable
{
    public class Entry
    {
        public string Name { get; init; } = "";
        public string Kind { get; init; } = "var";   // var/func/type/param/const
        public string Type { get; set; } = "int";    // ссылка на дескриптор типа/идентификатор типа
        public int ScopeLevel { get; init; } // уровень области

        public Entry(string name)
        { this.Name = name; }
    }

    /// <summary>
    /// Реализация генератора псевдослучайных чисел на основе ЛКГ (Линейный конгруэнтный генератор).
    /// </summary>
    public class PRNG
    {
        private long _seed;
        private long _lastValue;
        private List<uint> _lastValues;
        private uint _index;

        // Параметры ЛКГ, используемые в java.util.Random для надежности
        private const long a = 25214903917L;
        private const long c = 11L;
        private const long m = (1L << 48) - 1; // Маска для 48 бит (2^48 - 1)

        private const uint _j = 24;
        private const uint _k = 55;

        //Состояние и параметры для PCG (спер из интернета, мне не стыдно)
        private ulong _pcgState;
        private const ulong PCG_MULTIPLIER = 6364136223846793005UL;
        private const ulong PCG_INCREMENT = 1442695040888963407UL;
        /// <summary>
        /// Инициализирует новый экземпляр генератора.
        /// </summary>
        /// <param name="seed">Начальное значение. Если не указано, генерируется на основе системных параметров.</param>
        public PRNG(long? seed = null)
        {
            if (seed != null)
            { _seed = seed.Value; }
            else
            {
                long processId = Environment.ProcessId;
                long ticks = DateTime.Now.Ticks;
                _seed = processId ^ (int)ticks;
            }
            _pcgState = (ulong)_seed + PCG_INCREMENT;
            NextRawPCG();
            _index = (uint)(this.NextRawPCG() % _k);
            _lastValue = (_seed ^ a) & m;
            _lastValues = new List<uint>();
            for (long i = 0; i <= _k; i++)
            {
                _lastValues.Add(this.NextRawPCG());
            }

        }

        private int NextRawAdditive()
        {
            long jIndex = (_index - _j + _k) % _k;
            long kIndex = (_index + _j - _k) % _j;

            uint nextValue = _lastValues[(int)jIndex] + _lastValues[(int)kIndex];
            _lastValues[(int)_index] = nextValue;
            _index = (_index + 1) % _k;

            return unchecked((int)nextValue);
        }


        // Шаг PCG: обновление состояния + перемешивающая функция
        private uint NextRawPCG()
        {
            ulong oldState = _pcgState;
            _pcgState = oldState * PCG_MULTIPLIER + PCG_INCREMENT;

            // Перемешивающая функция XSH-RR
            uint xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
            int rot = (int)(oldState >> 59);
            return (xorshifted >> rot) | (xorshifted << (-rot & 31));
        }

        //Генерирует случайное очень случайное число
        private int NextRaw()
        {
            int pcgValue = (int)NextRawPCG();
            int additiveValue = NextRawAdditive();
            int state = pcgValue ^ additiveValue;

            //Операция перемешивания: в интернете сказали, что хорошо вносит энтропию
            state ^= (int)((uint)state >> 17);
            state ^= state << 31;
            state ^= (int)((uint)state >> 8);

            return (int)(state & 0xFFFFFFFF);
        }



        /// <summary>
        /// Возвращает неотрицательное (0-31) случайное целое число.
        /// </summary>
        public int Next()
        {
            return NextRaw() & 0x7FFFFFFF;
        }

        /// <summary>
        /// Возвращает случайное целое число, которое меньше указанного максимального значения.
        /// </summary>
        public int Next(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue), "maxValue должен быть неотрицательным.");
            return Next(0, maxValue);
        }

        /// <summary>
        /// Возвращает случайное целое число в указанном диапазоне.
        /// </summary>
        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue), "minValue не может быть больше maxValue.");

            long range = (long)maxValue - minValue;
            if (range == 0) return minValue;

            return (int)(minValue + (NextDouble() * range));
        }

        public double NextDouble()
        {
            // Используем полные 32 бита из NextRaw() напрямую (без потери энтропии)
            ulong hi = (ulong)(uint)NextRaw() << 21;  // Биты 52-21 (32 бита)

            // Используем младшие 21 бит из второго вызова
            ulong lo = (ulong)(uint)NextRaw() & ((1UL << 21) - 1);  // Биты 20-0 (21 бит)

            // Вывод в двоичной форме
            //Console.WriteLine($"hi:  {ToBinary64(hi)}");
            //Console.WriteLine($"lo:  {ToBinary64(lo)}");
            //Console.WriteLine($"OR:  {ToBinary64(hi | lo)}");
            //Console.WriteLine($"53:  {ToBinary64(1UL << 53)}");
            //Console.WriteLine();
            ulong bits53 = hi | lo;
            return (double)bits53 / (double)(1UL << 53);
        }

        public void DumpOneMillion(string path)
        {
            using var sw = new StreamWriter(path, false); // текстовый файл, одна выборка на строку
            var ci = CultureInfo.InvariantCulture;
            for (int i = 0; i < 1_000_000; i++)
            {
                double x = NextDouble();                 // ожидается в [0,1)
                sw.WriteLine(x.ToString("R", ci));       // формат "R" для полной точности double
            }
        }

        // Вспомогательная функция для вывода 64-битного числа в двоичном виде
        private static string ToBinary64(ulong value)
        {
            uint upper = (uint)(value >> 32);
            uint lower = (uint)(value & 0xFFFFFFFF);
            return Convert.ToString(upper, 2).PadLeft(32, '0') +
                   Convert.ToString(lower, 2).PadLeft(32, '0');
        }


        /// <summary>
        /// Возвращает случайное число с плавающей запятой в диапазоне [0.0f, 1.0f).
        /// </summary>
        public float NextSingle()
        {
            return (float)NextDouble();
        }

        /// <summary>
        /// Заполняет элементы указанного массива байтов случайными числами.
        /// </summary>
        public void NextBytes(byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)Next(256); // Генерируем число от 0 до 255
            }
        }
    }

    /// <summary>
    /// Базовый интерфейс для таблицы идентификаторов
    /// </summary>
    public interface IIdentifierTable
    {
        bool Insert(Entry entry);
        Entry? Search(string name);
        bool Delete(string name);
        int Count { get; }
        void PrintStatistics();
    }

    /// <summary>
    /// Таблица идентификаторов с открытой адресацией и рехешированием псевдослучайными числами на основе массива
    /// </summary>
    public class HashTableWithPRNG : IIdentifierTable
    {
        private class Slot
        {
            public Entry? Entry { get; set; }
            public bool IsDeleted { get; set; }
            public bool IsOccupied => Entry != null;
        }

        private Slot[] _table;
        private int _count;
        private int _capacity;
        private readonly PRNG _prng;
        private const double LOAD_FACTOR_THRESHOLD = 0.7;

        // Статистика для анализа
        private long _totalProbes;
        private int _insertions;
        private int _searches;
        private int _collisions;

        public int Count => _count;

        public HashTableWithPRNG(int initialCapacity = 128, long? seed = null)
        {
            // Размер таблицы - степень двойки для эффективности
            _capacity = GetNextPowerOfTwo(initialCapacity);
            _table = new Slot[_capacity];
            for (int i = 0; i < _capacity; i++)
                _table[i] = new Slot();

            _count = 0;
            _prng = new PRNG(seed);
        }

        private int GetNextPowerOfTwo(int n)
        {
            int power = 1;
            while (power < n) power <<= 1;
            return power;
        }

        /// <summary>
        /// Хеш-функция для строки (полиномиальная)
        /// </summary>
        private int Hash(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;

            int hash = 0;
            foreach (char c in key)
            {
                hash = ((hash << 5) - hash + c) & 0x7FFFFFFF;
            }
            return hash % _capacity;
        }

        /// <summary>
        /// Генерирует последовательность псевдослучайных смещений для ключа
        /// </summary>
        private IEnumerable<int> GetProbeSequence(string key)
        {
            // Создаем новый генератор с сидом на основе ключа для детерминированности
            int keySeed = key.GetHashCode();
            PRNG localPrng = new PRNG(keySeed);

            HashSet<int> visited = new HashSet<int>();
            int baseHash = Hash(key);

            // Генерируем до _capacity различных позиций
            for (int i = 0; i < _capacity; i++)
            {
                int offset = localPrng.Next(_capacity);
                int index = (baseHash + offset) % _capacity;

                // Избегаем повторов в последовательности
                if (!visited.Contains(index))
                {
                    visited.Add(index);
                    yield return index;
                }
            }
        }

        public bool Insert(Entry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Name))
                return false;

            // Если таблица перегружена - увеличиваем емкость
            if ((double)_count / _capacity >= LOAD_FACTOR_THRESHOLD)
            {
                Resize();
            }

            int probeCount = 0;
            //Тут происходит разрешение коллизии - если произошла коллизия - получаем следующее значение из последовательности
            foreach (int index in GetProbeSequence(entry.Name))
            {
                probeCount++;
                _totalProbes++;

                var slot = _table[index];

                // Нашли пустое место или удалённый слот
                if (!slot.IsOccupied || slot.IsDeleted)
                {
                    slot.Entry = entry;
                    slot.IsDeleted = false;
                    _count++;
                    _insertions++;

                    if (probeCount > 1)
                        _collisions++;

                    return true;
                }

                // Элемент уже существует
                if (slot.Entry.Name == entry.Name)
                {
                    slot.Entry = entry; // Обновляем
                    return true;
                }
            }

            // Не нашли свободного места (теоретически не должно случиться)
            return false;
        }

        public Entry? Search(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            int probeCount = 0;
            foreach (int index in GetProbeSequence(name))
            {
                probeCount++;
                _totalProbes++;

                var slot = _table[index];

                if (slot.IsOccupied && !slot.IsDeleted && slot.Entry.Name == name)
                {
                    _searches++;
                    return slot.Entry;
                }

                // Если слот никогда не занимался, элемента точно нет
                if (!slot.IsOccupied && !slot.IsDeleted)
                    break;
            }

            _searches++;
            return null;
        }

        public bool Delete(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            foreach (int index in GetProbeSequence(name))
            {
                var slot = _table[index];

                if (slot.IsOccupied && !slot.IsDeleted && slot.Entry.Name == name)
                {
                    slot.IsDeleted = true;
                    _count--;
                    return true;
                }

                if (!slot.IsOccupied && !slot.IsDeleted)
                    break;
            }

            return false;
        }

        private void Resize()
        {
            var oldTable = _table;
            _capacity *= 2;
            _table = new Slot[_capacity];

            for (int i = 0; i < _capacity; i++)
                _table[i] = new Slot();

            _count = 0;

            // Перехешируем все элементы
            foreach (var slot in oldTable)
            {
                if (slot.IsOccupied && !slot.IsDeleted)
                {
                    Insert(slot.Entry);
                }
            }
        }

        public void PrintStatistics()
        {
            Console.WriteLine($"\n=== Статистика хеш-таблицы с PRNG ===");
            Console.WriteLine($"Размер таблицы: {_capacity}");
            Console.WriteLine($"Элементов: {_count}");
            Console.WriteLine($"Коэффициент загрузки: {(double)_count / _capacity:F3}");
            Console.WriteLine($"Вставок: {_insertions}");
            Console.WriteLine($"Поисков: {_searches}");
            Console.WriteLine($"Коллизий: {_collisions}");
            Console.WriteLine($"Всего проб: {_totalProbes}");

            if (_insertions + _searches > 0)
            {
                double avgProbes = (double)_totalProbes / (_insertions + _searches);
                Console.WriteLine($"Среднее число проб: {avgProbes:F2}");
            }
        }
    }

    /// <summary>
    /// Таблица идентификаторов на основе простого списка
    /// </summary>
    public class SimpleListTable : IIdentifierTable
    {
        private List<Entry> _list;

        // Статистика
        private long _totalComparisons;
        private int _searches;
        private int _insertions;

        public int Count => _list.Count;

        public SimpleListTable()
        {
            _list = new List<Entry>();
        }

        public bool Insert(Entry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Name))
                return false;

            // Проверяем, нет ли уже такого элемента
            for (int i = 0; i < _list.Count; i++)
            {
                _totalComparisons++;
                if (_list[i].Name == entry.Name)
                {
                    _list[i] = entry; // Обновляем
                    _insertions++;
                    return true;
                }
            }

            // Добавляем новый
            _list.Add(entry);
            _insertions++;
            return true;
        }

        public Entry? Search(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            for (int i = 0; i < _list.Count; i++)
            {
                _totalComparisons++;
                if (_list[i].Name == name)
                {
                    _searches++;
                    return _list[i];
                }
            }

            _searches++;
            return null;
        }

        public bool Delete(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            for (int i = 0; i < _list.Count; i++)
            {
                _totalComparisons++;
                if (_list[i].Name == name)
                {
                    _list.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void PrintStatistics()
        {
            Console.WriteLine($"\n=== Статистика простого списка ===");
            Console.WriteLine($"Элементов: {_list.Count}");
            Console.WriteLine($"Вставок: {_insertions}");
            Console.WriteLine($"Поисков: {_searches}");
            Console.WriteLine($"Всего сравнений: {_totalComparisons}");

            if (_insertions + _searches > 0)
            {
                double avgComparisons = (double)_totalComparisons / (_insertions + _searches);
                Console.WriteLine($"Среднее число сравнений: {avgComparisons:F2}");
            }
        }
    }

    /// <summary>
    /// Таблица идентификаторов с открытой адресацией и использованием массива для разрешения проблем с адресацией
    /// </summary>
    /// <summary>
    /// Таблица идентификаторов с методом цепочек (separate chaining) на основе массива вёдер
    /// </summary>
    public class HashTableWithChaining : IIdentifierTable
    {
        private LinkedList<Entry>[] _buckets;
        private int _count;
        private int _capacity;
        private const double LOAD_FACTOR_THRESHOLD = 0.75;

        // Статистика для анализа
        private long _totalProbes;
        private int _insertions;
        private int _searches;
        private int _collisions;

        public int Count => _count;

        public HashTableWithChaining(int initialCapacity = 128)
        {
            // Размер таблицы - степень двойки для эффективности
            _capacity = GetNextPowerOfTwo(initialCapacity);
            _buckets = new LinkedList<Entry>[_capacity];
            _count = 0;
        }

        private int GetNextPowerOfTwo(int n)
        {
            int power = 1;
            while (power < n) power <<= 1;
            return power;
        }

        /// <summary>
        /// Хеш-функция для строки (полиномиальная)
        /// </summary>
        private int Hash(string key)
        {
            if (string.IsNullOrEmpty(key)) return 0;

            int hash = 0;
            foreach (char c in key)
            {
                hash = ((hash << 5) - hash + c) & 0x7FFFFFFF;
            }
            return hash % _capacity;
        }

        public bool Insert(Entry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Name))
                return false;

            // Если таблица перегружена - увеличиваем емкость
            if ((double)_count / _capacity >= LOAD_FACTOR_THRESHOLD)
            {
                Resize();
            }

            int index = Hash(entry.Name);
            _totalProbes++;

            // Инициализируем ведро, если оно ещё не создано
            if (_buckets[index] == null)
            {
                _buckets[index] = new LinkedList<Entry>();
            }

            var bucket = _buckets[index];
            int probeCount = 0;

            // Проверяем, существует ли уже элемент с таким ключом
            foreach (var existingEntry in bucket)
            {
                probeCount++;
                _totalProbes++;

                if (existingEntry.Name == entry.Name)
                {
                    // Обновляем существующий элемент
                    var node = bucket.Find(existingEntry);
                    if (node != null)
                    {
                        node.Value = entry;
                    }
                    return true;
                }
            }

            // Добавляем новый элемент в цепочку
            bucket.AddLast(entry);
            _count++;
            _insertions++;

            // Коллизия = если в ведре уже были элементы
            if (bucket.Count > 1)
            {
                _collisions++;
            }

            return true;
        }

        public Entry? Search(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            int index = Hash(name);
            _totalProbes++;
            _searches++;

            var bucket = _buckets[index];
            if (bucket == null)
                return null;

            // Линейный поиск в цепочке
            foreach (var entry in bucket)
            {
                _totalProbes++;

                if (entry.Name == name)
                {
                    return entry;
                }
            }

            return null;
        }

        public bool Delete(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            int index = Hash(name);
            var bucket = _buckets[index];

            if (bucket == null)
                return false;

            // Ищем и удаляем элемент из цепочки
            foreach (var entry in bucket)
            {
                if (entry.Name == name)
                {
                    bucket.Remove(entry);
                    _count--;
                    return true;
                }
            }

            return false;
        }

        private void Resize()
        {
            var oldBuckets = _buckets;
            int oldCapacity = _capacity;

            _capacity *= 2;
            _buckets = new LinkedList<Entry>[_capacity];
            _count = 0;


            int oldInsertions = _insertions;
            int oldCollisions = _collisions;

            // Перехешируем все элементы
            foreach (var bucket in oldBuckets)
            {
                if (bucket != null)
                {
                    foreach (var entry in bucket)
                    {
                        Insert(entry);
                    }
                }
            }


            _insertions = oldInsertions;
            _collisions = oldCollisions;
        }

        public void PrintStatistics()
        {
            Console.WriteLine($"\n=== Статистика хеш-таблицы с цепочками ===");
            Console.WriteLine($"Размер таблицы: {_capacity}");
            Console.WriteLine($"Элементов: {_count}");
            Console.WriteLine($"Коэффициент загрузки: {(double)_count / _capacity:F3}");
            Console.WriteLine($"Вставок: {_insertions}");
            Console.WriteLine($"Поисков: {_searches}");
            Console.WriteLine($"Коллизий: {_collisions}");
            Console.WriteLine($"Всего проб: {_totalProbes}");

            if (_insertions + _searches > 0)
            {
                double avgProbes = (double)_totalProbes / (_insertions + _searches);
                Console.WriteLine($"Среднее число проб: {avgProbes:F2}");
            }

            // Дополнительная статистика по распределению цепочек
            int emptyBuckets = 0;
            int maxChainLength = 0;
            double totalChainLength = 0;
            int nonEmptyBuckets = 0;

            foreach (var bucket in _buckets)
            {
                if (bucket == null || bucket.Count == 0)
                {
                    emptyBuckets++;
                }
                else
                {
                    nonEmptyBuckets++;
                    int chainLength = bucket.Count;
                    totalChainLength += chainLength;
                    if (chainLength > maxChainLength)
                        maxChainLength = chainLength;
                }
            }

            Console.WriteLine($"Пустых вёдер: {emptyBuckets}");
            Console.WriteLine($"Заполненных вёдер: {nonEmptyBuckets}");
            Console.WriteLine($"Максимальная длина цепочки: {maxChainLength}");

            if (nonEmptyBuckets > 0)
            {
                Console.WriteLine($"Средняя длина цепочки: {totalChainLength / nonEmptyBuckets:F2}");
            }
        }
    }


}

