using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;

// Класс для пользователя
public class User
{
    public string Name { get; set; }
    public int CPM { get; set; }
    public double CPS { get; set; }
}

// Класс для таблицы рекордов
public class Leaderboard
{
    public List<User> Users { get; set; }

    public Leaderboard()
    {
        Users = new List<User>();
    }

    public void AddUser(User user)
    {
        Users.Add(user);
    }

    public string Serialize()
    {
        return JsonConvert.SerializeObject(Users);
    }

    public static Leaderboard Deserialize(string data)
    {
        return new Leaderboard
        {
            Users = JsonConvert.DeserializeObject<List<User>>(data)
        };
    }
}

// Класс для набора текста
public class TypingTest
{
    private readonly string _text;
    private string _userInput;
    private DateTime _startTime;
    private DateTime _endTime;

    public TypingTest(string text)
    {
        _text = text;
    }

    public void Start()
    {
        Console.WriteLine("\n" + _text);
        _userInput = "";

        _startTime = DateTime.Now;
        ConsoleKeyInfo keyInfo;
        do
        {
            keyInfo = Console.ReadKey(true);
            if (keyInfo.Key != ConsoleKey.Enter)
            {
                Console.Write(keyInfo.KeyChar);
                _userInput += keyInfo.KeyChar;
            }
        } while (keyInfo.Key != ConsoleKey.Enter);

        _endTime = DateTime.Now;
    }

    public double CalculateCPM()
    {
        var duration = _endTime - _startTime;
        var minutes = duration.TotalMinutes;
        if (minutes > 0)
            return _userInput.Length / minutes;
        return 0;
    }

    public double CalculateCPS()
    {
        var duration = _endTime - _startTime;
        var seconds = duration.TotalSeconds;
        if (seconds > 0)
            return _userInput.Length / seconds;
        return 0;
    }

    public void Run()
    {
        Console.WriteLine("Тест начался! Наберите следующий текст:");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Start();
        Console.ResetColor();

        Console.WriteLine($"\nВремя выполнения: {(_endTime - _startTime).TotalSeconds:F2} сек.");
        Console.WriteLine($"Ваш результат: CPM - {CalculateCPM():F2}, CPS - {CalculateCPS():F2}");
    }
}

// Класс для отсчета таймера
public class Timer
{
    private readonly int _timeLimit;
    private int _countdown;

    public Timer(int timeLimit)
    {
        _timeLimit = timeLimit;
    }

    public void Countdown()
    {
        Console.WriteLine("Тест начнется через:");
        for (_countdown = 3; _countdown >= 0; _countdown--)
        {
            Console.Write("\r{0}...   ", _countdown);
            Thread.Sleep(1000);
        }
        Console.WriteLine();
    }

    public void Run()
    {
        Console.WriteLine("Печатайте!");
        for (_countdown = _timeLimit; _countdown > 0; _countdown--)
        {
            Console.Write("\rОсталось времени: {0} сек.", _countdown);
            Thread.Sleep(1000);
        }
        Console.WriteLine("\nВремя вышло!");
    }
}

// Главный класс
public class Program
{
    private const string LeaderboardFilePath = "leaderboard.json";
    private const int MaxWords = 20;

    public static void Main(string[] args)
    {
        Leaderboard leaderboard;

        // Загрузка таблицы рекордов из файла (если есть)
        try
        {
            var data = File.ReadAllText(LeaderboardFilePath);
            leaderboard = Leaderboard.Deserialize(data);
        }
        catch (FileNotFoundException)
        {
            leaderboard = new Leaderboard();
        }

        while (true)
        {
            Console.WriteLine("Введите ваше имя:");
            var name = Console.ReadLine();

            var test = new TypingTest(GenerateRandomText());
            var timer = new Timer(60);

            // Начало теста печатания
            Console.WriteLine("Готовы к тесту? (да/нет)");
            var ready = Console.ReadLine();
            if (ready.ToLower() != "да")
                continue;

            timer.Countdown();
            var testThread = new Thread(test.Run);
            var timerThread = new Thread(timer.Run);

            timerThread.Start();
            testThread.Start();

            testThread.Join();
            timerThread.Join();

            // Вывод результатов и обновление таблицы рекордов
            var cpm = test.CalculateCPM();
            var cps = test.CalculateCPS();
            var user = new User { Name = name, CPM = (int)cpm, CPS = cps };
            leaderboard.AddUser(user);

            Console.WriteLine($"Ваш результат: CPM - {cpm:F2}, CPS - {cps:F2}");

            // Сохранение таблицы рекордов в файл
            var data = leaderboard.Serialize();
            File.WriteAllText(LeaderboardFilePath, data);

            // Вывод таблицы рекордов
            PrintLeaderboard(leaderboard);

            // Повторный тест или завершение программы
            Console.WriteLine("Хотите повторить тест? (да/нет)");
            var repeat = Console.ReadLine();
            if (repeat.ToLower() != "да")
            {
                break;
            }
        }
    }

    private static void PrintLeaderboard(Leaderboard leaderboard)
    {
        Console.WriteLine("Таблица рекордов:");
        Console.WriteLine("Имя\tCPM\tCPS");
        foreach (var user in leaderboard.Users)
        {
            Console.WriteLine($"{user.Name}\t{user.CPM}\t{user.CPS:F2}");
        }
    }

    private static string GenerateRandomText()
    {
        var words = new List<string> {
            "сегодня", "вечером", "прогулка", "по", "парку",
            "вопреки", "погоде", "осень", "была", "теплая",
            "на", "небе", "висел", "огромный", "яркий",
            "луна", "просыпаясь", "все", "дольше", "буду"
        };

        var random = new Random();
        var wordCount = random.Next(1, MaxWords + 1);

        var selectedWords = words.OrderBy(x => random.Next()).Take(wordCount);

        return string.Join(" ", selectedWords);
    }
}
