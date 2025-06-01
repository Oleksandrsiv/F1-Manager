namespace CarsLib;

using CarsLib;

public interface IRaceInterface
{
    void AskPlayerPace();
    void SimulateLap();
    void ShowRaceStatistics();
    void OfferPitStop();
    void ShowFinalResults();
    void ShowLapResults();
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
        while (true)
        {
            Console.WriteLine("Choose your race pace:");
            Console.WriteLine("1. Aggressive");
            Console.WriteLine("2. Normal");
            Console.WriteLine("3. Economic");
            string paceInput = Console.ReadLine();

            if (byte.TryParse(paceInput, out byte pace))
            {
                _playerData.Car.SetPace(pace, out bool isValid);
                if (isValid) break;
            }

            Console.WriteLine("Invalid value. Please try again.");
        }
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
                Console.WriteLine($"[AI] {car.Car.Team} | Pace: {car.Car.Pace} | Tire: {car.Car.TypeOfTire} | TireCond: {car.Car.TireCondition:F1}% | Fuel: {car.Car.CurrentAmountOfFuel:F1}L");
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
        Console.WriteLine("Do you want to make a pit stop?");
        Console.WriteLine("Press 1 for yes, any other key for no.");
        string input = Console.ReadLine();

        if (input != "1")
        {
            Console.WriteLine("You pass the pit lane.\n");
            return;
        }

        Console.WriteLine("Pit stop started.");

        byte? newTire = null;

        // tire
        while (true)
        {
            Console.WriteLine("Choose tire type:");
            Console.WriteLine("1. Soft");
            Console.WriteLine("2. Medium");
            Console.WriteLine("3. Hard");
            Console.WriteLine("4. Wet");
            string tyreInput = Console.ReadLine();

            newTire = tyreInput switch
            {
                "1" => 1,
                "2" => 2,
                "3" => 3,
                "4" => 4,
                _ => null
            };

            if (newTire == null)
            {
                Console.WriteLine("Invalid tire selection. Please try again.");
            }
            else
            {
                break;
            }
        }

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

            if (fuelToAdd <= 0)
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

        // pit
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


    public void ShowLapResults()
{
    var rankedCars = _raceManager.GetSortedCars();
    double leaderTime = rankedCars.First().TotalRaceTime;

    foreach (var car in rankedCars)
    {
        string lapTimeText;
        string gapText = "";

        if (car.Car.Dnf)
        {
            lapTimeText = "DNF";
            Console.WriteLine($"{car.Car.Team,-10} | Tire: {car.Car.GetTireName(),-6} | Time: {lapTimeText}");
        }
        else
        {
            lapTimeText = $"{car.LastLapTime:F2}s ({car.Car.TireCondition}%)";
            Console.Write($"{car.Car.Team,-10} | Tire: {car.Car.GetTireName(),-6} | Time: {lapTimeText}");

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



public static class CarFactory
{
    private static readonly Random _rand = new();

    public static CarF1 CreateRandomizedCar(string team)
    {
        int topSpeed = ApplyVariance(310, 5); // base speed 310 
        double mileage = 0.0;
        byte pace = 2; // normal
        int maxVolumeOfTank = ApplyVariance(100, 5); // 100 L
        double baseFuelConsumption1km = ApplyVarianceDouble(0.85, 5); // fuel consumption 0.85 L/km
        double amountOfFuel = ApplyVarianceDouble(90.0, 5); // starting fuel
        byte typeOfTire = 1; // Soft
        byte tireCondition = 100;

        return new CarF1(team, topSpeed, mileage, pace, maxVolumeOfTank, baseFuelConsumption1km, amountOfFuel, typeOfTire, tireCondition);
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

