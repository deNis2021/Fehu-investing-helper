using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;

public class WeatherDay
{
    public string Date { get; set; }
    public string DayOfWeek { get; set; }
    public string Temperature { get; set; }
    public string Description { get; set; }

    public override string ToString()
    {
        return $"{Date} ({DayOfWeek}): {Temperature} — {Description}";
    }
}

class Program
{
    private static readonly HttpClient client = new HttpClient();

    static async Task<List<WeatherDay>> GetWeather(string city, int days)
    {
        try
        {
            Console.WriteLine("Отримую дані про погоду...");
            Console.WriteLine("Питай місцевих чукч...");
            Console.WriteLine("Аналізую ретроградність меркурію...");
            Console.WriteLine("З'єднуюсь з богом для підтвердження результатів...");
            Console.WriteLine("Виводжу результати...");

            string cityUrl = city.Trim().ToLower().Replace(' ', '-');
            string url;

            if (days == 10)
            {
                url = $"https://sinoptik.ua/pohoda/{cityUrl}/10-dniv"; 
            }
            else
            {
                url = $"https://sinoptik.ua/pohoda/{cityUrl}"; 
            }

            using HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string html = await response.Content.ReadAsStringAsync();

            List<WeatherDay> forecast = new();

            string pattern = @"<a class=""tkK415TH(?: OGO-yOID)?""[^>]*>(.*?)</a>";
            var matches = Regex.Matches(html, pattern, RegexOptions.Singleline);

            foreach (Match match in matches)
            {
                string block = match.Groups[1].Value;

                string dayOfWeek = GetValue(block, @"<p class=""xM6dxfW4"">(.*?)</p>");
                string date = GetValue(block, @"<p class=""RSWdP9mW(?: X6TmI5bI)?""[^>]*>(.*?)</p>");
                string month = GetValue(block, @"<p class=""yQxWb1P4"">(.*?)</p>");
                string desc = GetValue(block, @"aria-label=""(.*?)""");
                string tempMin = GetValue(block, @"<p class=""\+Ovk0iEc"">мін\.</p><p>(.*?)</p>");
                string tempMax = GetValue(block, @"<p class=""\+Ovk0iEc"">макс\.</p><p>(.*?)</p>");

                if (!string.IsNullOrEmpty(date) && !string.IsNullOrEmpty(month))
                {
                    forecast.Add(new WeatherDay
                    {
                        Date = $"{date} {month}",
                        DayOfWeek = dayOfWeek,
                        Temperature = $"{tempMin} / {tempMax}",
                        Description = desc
                    });
                }
            }

            return forecast;
        }

        catch (HttpRequestException e)
        {
            Console.WriteLine($"Помилка запиту: {e.Message}");
            return new List<WeatherDay>();
        }

        catch (Exception e)
        {
            Console.WriteLine($"Інша помилка: {e.Message}");
            return new List<WeatherDay>();
        }
    }

    static string GetValue(string input, string pattern)
    {
        var match = Regex.Match(input, pattern, RegexOptions.Singleline);

        if (match.Success && match.Groups.Count > 1)
        {
            return match.Groups[1].Value.Trim().Replace("&deg;", "°");
        }
        return "";
    }

    static async Task SaveToFile(string city, List<WeatherDay> forecast)
    {
        string fileName = $"extracted_weather.txt";
        using StreamWriter writer = new StreamWriter(fileName);

        foreach (var day in forecast)
        {
            await writer.WriteLineAsync(day.ToString());
        }
        Console.WriteLine($"\n Прогноз збережено у файл: {fileName}");
    }

    static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        while (true)
        {
            Console.WriteLine("Оберіть дію:");
            Console.WriteLine("1 - Отримати прогноз погоди");
            Console.WriteLine("0 - Вийти");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    Console.Write("Введіть назву міста англійською (Mariupol або Donetsk): ");
                    string city = Console.ReadLine();

                    Console.Write("Виберіть період прогнозу (7 або 10 днів): ");
                    string daysInput = Console.ReadLine();
                    int days;

                    if (!int.TryParse(daysInput, out days) || (days != 7 && days != 10))
                    {
                        Console.WriteLine("Невірний вибір. Будь ласка, введіть 7 або 10.");
                        break;
                    }

                    var forecast = await GetWeather(city, days);

                    if (forecast.Count == 0)
                    {
                        Console.WriteLine("Не вдалося отримати прогноз. Перевірте назву міста або період.");
                        break;
                    }

                    Console.WriteLine($"\nПогода в місті ({days} днів):");
                    foreach (var day in forecast)
                    {
                        Console.WriteLine(day);
                    }

                    await SaveToFile(city, forecast);
                    break;

                case "0":
                    return;

                default:
                    Console.WriteLine("Невірний вибір. Спробуйте ще раз.");
                    break;
            }
        }
    }
}