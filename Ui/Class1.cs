namespace CarsLib;

using CarsLib;

public interface IRaceInterface
{
    void AskPlayerPace();
    void SimulateLap();
    void ShowRaceStatistics();
    void OfferPitStop();
    void ShowFinalResults();
    void ShowLapResults(int lap, int totalLaps);
}

public class RaceInterface : IRaceInterface
{
    private readonly CarRaceData _playerData;
    private readonly RaceManager _raceManager;
    private readonly Track _track;
    private readonly WeatherManager _weather;
    private readonly AIController _aiController;
    private int _currentLap;
    private int _totalLaps;
    Arrows arrows = new Arrows();

    public RaceInterface(CarRaceData playerCarData, RaceManager raceManager, Track track, WeatherManager weather, int totalLaps)
    {
        _playerData = playerCarData;
        _raceManager = raceManager;
        _track = track;
        _weather = weather;
        _totalLaps = totalLaps;
        _aiController = new AIController();
    }

    public void AskPlayerPace()
    {
        string[] paceOptions = { "Aggressive", "Normal", "Economic" };
        int selection = arrows.ShowArrowMenu("Choose your race pace:", paceOptions);

        _playerData.Car.SetPace((byte)(selection + 1), out bool isValid);
        Console.WriteLine($"You selected: {paceOptions[selection]}");
        Thread.Sleep(1000);
    }

    public void SimulateLap()
    {
        _currentLap++;

        foreach (var car in _raceManager.CarsOnTrackReadOnly)
        {
            car.IdealLapTime = _track.CalculateIdealTime(car.Car.TopSpeed);
        }

        foreach (var car in _raceManager.CarsOnTrackReadOnly)
        {
            if (car != _playerData && !car.Car.Dnf)
            {
                _aiController.AiMakeDecision(car, _track, _weather, _currentLap, _totalLaps);
            }
        }

        foreach (var car in _raceManager.CarsOnTrackReadOnly)
        {
            if (!car.Car.Dnf)
            {
                car.Car.SetMultipliers(_weather);
                bool ride = car.Car.Ride(_track.LengthKm);
                if (!ride)
                {
                    car.Car.Dnf = true;
                    continue;
                }

                double lapTime = car.Car.CalculateLapTime(car.IdealLapTime);
                car.UpdateList(lapTime, _track, _weather);
            }
        }
    }

    public void ShowRaceStatistics()
    {
        Console.WriteLine(new string('-', 50));
        Console.WriteLine($"Weather: {_weather.CurrentWeather}, Temperature: {_weather.TemperatureCelsius}°C");
        Console.WriteLine("Player car status:");
        Console.WriteLine($"  Fuel: {_playerData.Car.CurrentAmountOfFuel:F1} / {_playerData.Car.MaxVolumeOfTank} L");
        Console.WriteLine($"  Tires: {_playerData.Car.GetTireName()}, Wear: {_playerData.Car.TireCondition}%");
        System.Console.WriteLine($" Pace: {_playerData.Car.Pace}");
    }

    public void OfferPitStop()
    {
        string[] confirmOptions = { "Yes", "No" };
        int confirm = arrows.ShowArrowMenu("Do you want to make a pit stop?", confirmOptions);

        if (confirm != 0)
        {
            Console.WriteLine("You pass the pit lane.\n");
            Thread.Sleep(200);
            return;
        }

        Console.WriteLine("Pit stop started.");
        // tire
        string[] tireOptions = { "Soft", "Medium", "Hard", "Wet" };
        int tireSelection = arrows.ShowArrowMenu("Choose tire type:", tireOptions);
        byte? newTire = (byte)(tireSelection + 1);

        //fuel 
        double fuelToAdd;
        double availableSpace = _playerData.Car.MaxVolumeOfTank - _playerData.Car.CurrentAmountOfFuel;
        while (true)
        {
            Console.WriteLine($"How many liters of fuel to add? (max: {availableSpace} L)");
            string fuelInput = Console.ReadLine();

            if (!double.TryParse(fuelInput, out fuelToAdd))
            {
                Console.WriteLine("Error: Invalid number entered. Please try again.");
                continue;
            }

            if (fuelToAdd < 0)
            {
                Console.WriteLine("Please enter a positive number.");
                continue;
            }

            if (fuelToAdd > availableSpace)
            {
                Console.WriteLine("Too much! Not enough space in the tank.");
                continue;
            }

            break;
        }

        // box
        _playerData.Car.PitStop(fuelToAdd, _weather, newTire);
        Console.WriteLine($"Pit stop complete. {fuelToAdd} L of fuel added.");
    }


    public void ShowFinalResults()
    {
        Console.Clear();
        Console.WriteLine("\nRace finished!");
        Console.WriteLine(new string('-', 40));

        List<CarRaceData> results = _raceManager.GetSortedCars();

        int position = 1;
        foreach (var car in results)
        {
            string result = car.Car.Dnf ? "DNF" : $"{car.TotalRaceTime:F2}s";
            Console.WriteLine($"{position++}. {car.Car.Team,-10} | Result: {result}");
        }
    }


    public void ShowLapResults(int lap, int totalLaps)
    {
        Console.WriteLine($"\nlap {lap}/{totalLaps}");
        Console.WriteLine(new string('-', 50));

        var rankedCars = _raceManager.GetSortedCars();
        double leaderTime = rankedCars.First().TotalRaceTime;

        foreach (var car in rankedCars)
        {
            string lapTimeText;
            string gapText = "";

            if (car.Car.Dnf)
            {
                lapTimeText = "DNF";
                Console.WriteLine($"{car.Car.Team,-10} | - Tire: {car.Car.GetTireName(),-6} | - Time: {lapTimeText}");
            }
            else
            {
                lapTimeText = $"{car.LastLapTime:F2}s ({car.Car.TireCondition}%)";
                Console.Write($"{car.Car.Team,-10} | - Tire: {car.Car.GetTireName(),-6} | - Time: {lapTimeText}");

                if (car != rankedCars.First())
                {
                    double gap = car.TotalRaceTime - leaderTime;
                    gapText = $"+{gap:F2}s";

                    // set gap color
                    if (gap <= 1)
                        Console.ForegroundColor = ConsoleColor.Green;
                    else if (gap <= 3)
                        Console.ForegroundColor = ConsoleColor.White;
                    else
                        Console.ForegroundColor = ConsoleColor.Red;

                    Console.Write("  Gap: ");
                    Console.Write(gapText);
                    Console.ResetColor();
                }

                Console.WriteLine();
            }

            Console.ResetColor();
        }
    }

}

public class Arrows
{
    public int ShowArrowMenu(string title, string[] options)
    {
        int selectedIndex = 0;
        ConsoleKey key;

        do
        {
            Console.Clear();
            Console.WriteLine(title);
            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow; 
                    Console.WriteLine($"> {options[i]}");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine($"  {options[i]}");
                }
            }

            key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.UpArrow && selectedIndex > 0)
                selectedIndex--;
            else if (key == ConsoleKey.DownArrow && selectedIndex < options.Length - 1)
                selectedIndex++;

        } while (key != ConsoleKey.Enter);

        return selectedIndex;
    }
}



public static class CarFactory
{
    const int DefaultTopSpeed = 310;
    const int VariancePercent = 5;
    const int TankCapacity = 100;
    const double BaseFuelPerKm = 0.85;
    const double StartingFuel = 90;
    const byte DefaultTire = 1;
    const byte FullTireCondition = 100;

    private static readonly Random _rand = new();

    public static CarF1 CreateRandomizedCar(string team)
    {
        int topSpeed = ApplyVariance(DefaultTopSpeed, VariancePercent); 
        int maxVolumeOfTank = ApplyVariance(TankCapacity, VariancePercent); 
        double baseFuelConsumption1km = ApplyVarianceDouble(BaseFuelPerKm, VariancePercent); 
        double amountOfFuel = ApplyVarianceDouble(StartingFuel, VariancePercent); 
        byte typeOfTire = DefaultTire; 
        byte tireCondition = FullTireCondition;

        return new CarF1(team, topSpeed, maxVolumeOfTank, baseFuelConsumption1km, amountOfFuel, typeOfTire, tireCondition);
    }


    private static int ApplyVariance(int baseValue, int percent)
    {
        double variance = baseValue * percent / 100.0;
        return baseValue + _rand.Next(-(int)variance, (int)variance + 1);
    }

    private static double ApplyVarianceDouble(double baseValue, int percent)
    {
        double variance = baseValue * percent / 100.0;
        return Math.Round(baseValue + (_rand.NextDouble() * 2 - 1) * variance, 2);
    }
}

