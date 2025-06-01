namespace CarsLib;


public class Track
{
    public double LengthKm { get; private set; }
    public int EasyTurns { get; private set; }
    public int MediumTurns { get; private set; }
    public int HardTurns { get; private set; }

    private const double EasyTurnPenalty = 0.5;
    private const double MediumTurnPenalty = 1.5;
    private const double HardTurnPenalty = 3.0;

    private static Random random = new Random();
    private WeatherManager _weatherManager;

    public Track(WeatherManager weatherManager)
    {
        // length of track
        LengthKm = Math.Round(random.NextDouble() * (7 - 3) + 3, 2);//check

        // turns
        EasyTurns = random.Next(5, 15);
        MediumTurns = random.Next(3, 10);
        HardTurns = random.Next(1, 5);

        _weatherManager = weatherManager;
        _weatherManager.GenerateWeather();
    }




    // ideal time 
    public double CalculateIdealTime(double baseSpeedKmh)
    {
        // time without penalty
        double baseTimeMin = LengthKm / baseSpeedKmh * 60;


        // time with penalty by corners
        double timeWithTurnPenalty = (EasyTurns * EasyTurnPenalty) + (MediumTurns * MediumTurnPenalty) + (HardTurns * HardTurnPenalty);
        
        return baseTimeMin + timeWithTurnPenalty;
    }
}


public class CarF1
{
    public string Team { get; private set; }
    public double TopSpeed { get; private set; }
    public double Mileage { get; private set; }
    public byte Pace { get;  set; }

    //fuel
    public int MaxVolumeOfTank { get; private set; }
    public double CurrentAmountOfFuel { get; private set; }
    private double BaseFuelConsumption1km;

    //tire
    public byte TypeOfTire { get; private set; }
    public byte TireCondition { get; private set; }
    private double _tireMultiplier;
    private double _baseTireConsumption;
    
    private double _paceMultiplier;
    private double _typeWearMultiplier;
    private byte _pitStopPenalty = 0;

    public bool Dnf = false;
    public bool IsPitStop = false;
    


    public CarF1(string team, int topSpeed, double mileage, byte pace,
        int maxVolumeOfTank, double baseFuelConsumption1km, double amountOfFuel,
        byte typeOfTire, byte tireCondition)
    {
        Team = team;
        TopSpeed = topSpeed;
        Mileage = mileage;
        Pace = pace;
        MaxVolumeOfTank = maxVolumeOfTank;
        BaseFuelConsumption1km = baseFuelConsumption1km;
        CurrentAmountOfFuel = amountOfFuel;
        TypeOfTire = typeOfTire;
        TireCondition = tireCondition;
    }
    public void SetPace(byte pace, out bool isValid)
    {
    isValid = true;

    if (pace < 1 || pace > 3)
    {
        Console.WriteLine("invalid pace! Choose between 1 (Economic), 2 (Normal) or 3 (Aggressive).");
        isValid = false;
        return;
    }

    Pace = pace;
    }
    
    public void SetTireType(byte typeOfTire)
    {
       
        if (typeOfTire < 1 || typeOfTire > 4)
        {
            Console.WriteLine("Invalid tire type! Choose between 1 (Soft), 2 (Medium), 3 (Hard) or 4 (Wet).");
            return;
        }
        TypeOfTire = typeOfTire;
        TireCondition = 100; 
    }

    public string GetTireName()
    {
        return TypeOfTire switch
        {
            1 => "Soft",
            2 => "Medium",
            3 => "Hard",
            4 => "Wet",
            _ => "Unknown"
        };
    }

    public void SetMultipliers(WeatherManager weatherManager)
    {
    var temperature = weatherManager.TemperatureCelsius;
    var weather = weatherManager.CurrentWeather;
    

    _paceMultiplier = Pace switch
    {
        1 => 0.9, // aggressive
        2 => 1.0, // normal
        3 => 1.3, // economic
        _ => 1.0 // default to normal
    };

    _tireMultiplier = (100 - TireCondition) / 100.0;

    (_baseTireConsumption, _typeWearMultiplier) = TypeOfTire switch // S M H W | 1 2 3 4
    {
        1 => (0.8, 1.2),  // soft
        2 => (0.5, 1.0),  // medium
        3 => (0.2, 0.8),  // hard
        4 => (0.6, 0.6),  // wet
        _ => (0, 0)   // 
    };


    // weather penalty
    double weatherPenalty = WeatherManager.TyreWeather(temperature) / 100.0;

    if ((TypeOfTire <= 3) && (weather == "Rainy"))
    {
        _tireMultiplier += 0.5;
        _typeWearMultiplier += 0.5;
    }

    if ((TypeOfTire == 4 || TypeOfTire == 5) && weather == "Sunny")
    {
        _tireMultiplier += 0.3;
        _typeWearMultiplier += 0.3;
    }

    _typeWearMultiplier *= weatherPenalty; 

    // speed penalty 
    TopSpeed = TopSpeed * (1 - _tireMultiplier); 
    }

    public double FuelConsumption1km => BaseFuelConsumption1km * _paceMultiplier;  
    public double TireConsumption1km => _baseTireConsumption * _paceMultiplier * _typeWearMultiplier * (1 + _tireMultiplier);

    public bool Ride(double distanceKm)
    {
        double fuelNeeded = FuelConsumption1km * distanceKm;
        if (CurrentAmountOfFuel < fuelNeeded)   
        { 
         return false;
        }
        CurrentAmountOfFuel -= fuelNeeded;
        Mileage += distanceKm;
        double tireWear = TireConsumption1km * distanceKm; 
        TireCondition = (byte)Math.Max(0, TireCondition - tireWear);
        return true;
    }

    public bool Refill(double amountOfRefilledFuel)
    {
    if (amountOfRefilledFuel <= 0)
    {
        Console.WriteLine("Refill amount must be positive!");
        return false;
    }

    if (CurrentAmountOfFuel + amountOfRefilledFuel > MaxVolumeOfTank)
    {
        Console.WriteLine("Incorrect volume! Fuel tank overflow.");
        return false;
    }

    CurrentAmountOfFuel += amountOfRefilledFuel;
    return true;
    }


    public void PitStop(double amountOfRefilledFuel,WeatherManager weatherManager, byte? newTireType = null)
    {
        IsPitStop = true;
        _pitStopPenalty = 0;

        if (amountOfRefilledFuel > 0)
        {
            Refill(amountOfRefilledFuel);
            _pitStopPenalty += 10;
        }

       if (newTireType.HasValue && newTireType >= 1 && newTireType <= 4)
       {
            SetTireType(newTireType.Value);
            _pitStopPenalty += 12;
       }
        


        SetMultipliers(weatherManager);
    }



    public double CalculateLapTime(double idealTime)
    {
        CarF1 car = this;
        if (car.Dnf) 
        {
            return double.MaxValue; // DNF, return min value
        }


        double result = idealTime  * _paceMultiplier * (1 + _tireMultiplier); 

        if (IsPitStop)
        {
            result += _pitStopPenalty;
            _pitStopPenalty = 0;
            IsPitStop = false;
        }

        return result;
    }
}

public static class IncidentManager
{
    private static Random random = new Random();

    public static bool CheckForIncident(CarF1 car, Track track, WeatherManager weather)
    {
        double totalProbability = 0.0;

        bool isWet = weather.CurrentWeather == "Rainy";
        bool tireMismatch = isWet && car.TypeOfTire != 4; // if rainy and without Wet

        if (tireMismatch)
        {
            // if rainy and without Wet  (from 20 to 40%)
            totalProbability += 0.2 + ((100 - car.TireCondition) / 100.0) * 0.2;
        }
        else
        {
            if (car.TireCondition < 40)
            {
                double tirePenalty = (40 - car.TireCondition) / 60.0; // from 0 to 1
                totalProbability += 0.05 * tirePenalty; // мax 5%
            }
        }

        return random.NextDouble() < totalProbability;
    }
}



public class CarRaceData
{
    public CarF1 Car { get; private set; }
    public double LastLapTime { get; private set; }
    public double TotalRaceTime { get; private set; }
    public double IdealLapTime { get;  set; }
    public double PreviousLapTime { get; private set; }


    public CarRaceData(CarF1 car)
    {
        Car = car;
    }

    public void UpdateList(double lapTime, Track track, WeatherManager weather)
    {
        Car.Dnf = IncidentManager.CheckForIncident(Car, track, weather); 

        if (!Car.Dnf) 
        {
            PreviousLapTime = LastLapTime;
            LastLapTime = lapTime;
            TotalRaceTime += lapTime;
        }
    }


    
    public void SetMultipliers(WeatherManager weatherManager)
    {
    Car.SetMultipliers(weatherManager);
    }
}

public class RaceManager
{
    private readonly List<CarRaceData> carsOnTrack = new();

    public void RegisterCar(CarRaceData car) => carsOnTrack.Add(car);

    public void SortListOfCars()
    {
        var sorted = carsOnTrack.OrderBy(c => c.TotalRaceTime).ToList();
        carsOnTrack.Clear();
        carsOnTrack.AddRange(sorted);
    }
    
    public List<CarRaceData> GetSortedCars()
    {
        return carsOnTrack
            .OrderBy(car => car.Car.Dnf) // Спочатку всі, де Dnf == false (фінішували), потім true (DNF)
            .ThenBy(c => c.TotalRaceTime)
            .ToList();
    }




    public void ApplyMultipliersToAllCars(WeatherManager weatherManager)
    {
        foreach (var car in carsOnTrack)
        {
            car.SetMultipliers(weatherManager);
        }

    }

    public IReadOnlyList<CarRaceData> CarsOnTrackReadOnly => carsOnTrack.AsReadOnly();
}


    public class RaceSession
    {
        private readonly Track _track;
        private readonly WeatherManager _weatherManager;
        private readonly RaceManager _raceManager;
        private readonly LapSimulator _lapSimulator;

        public RaceSession()
        {
            _weatherManager = new WeatherManager();
            _track = new Track(_weatherManager); 
            _raceManager = new RaceManager();
            _lapSimulator = new LapSimulator(_track, _weatherManager);
        }

        public void RegisterCar(CarF1 car)
        {
            var raceData = new CarRaceData(car);
            raceData.IdealLapTime = _track.CalculateIdealTime(car.TopSpeed);
            _raceManager.RegisterCar(raceData);
        }

        public bool SimulateLap()
        {
            bool weatherChanged = _weatherManager.UpdateWeather(_raceManager.CarsOnTrackReadOnly.ToList());
            bool allFinished = true;

            foreach (var carData in _raceManager.CarsOnTrackReadOnly)
            {
                bool lapFinished = _lapSimulator.SimulateLap(carData);
                if (!lapFinished)
                {
                    carData.Car.Dnf = true;
                    allFinished = false;
                }
            }

            return allFinished;
        }

        public IReadOnlyList<CarRaceData> GetRaceData() => _raceManager.CarsOnTrackReadOnly;

        public Track Track => _track;
        public WeatherManager Weather => _weatherManager;
    }



    public class LapSimulator
    {
        private readonly Track _track;
        private readonly WeatherManager _weather;

        public LapSimulator(Track track, WeatherManager weather)
        {
            _track = track;
            _weather = weather;
        }

        public bool SimulateLap(CarRaceData data)
        {
            data.SetMultipliers(_weather);


            if (data.Car.Dnf) return false;

            bool success = data.Car.Ride(_track.LengthKm);
            if (!success) return false;

            double lapTime = data.Car.CalculateLapTime(data.IdealLapTime);

            data.UpdateList(lapTime, _track, _weather);
            return true;
        }
    }

    public class WeatherManager
    {
        private static Random random = new Random();
        public string CurrentWeather { get; private set; }
        public byte TemperatureCelsius { get; private set; }
        private byte _weatherChangeInterval;
        private byte _lapsSinceWeatherChange = 0;

        public WeatherManager()
        {
            GenerateWeather();
        }

        public void GenerateWeather()
        {
            byte weatherIndex = (byte)random.Next(0, 3);
            CurrentWeather = weatherIndex switch
            {
                0 => "Sunny",
                1 => "Rainy",
                _ => "Sunny"
            };

            TemperatureCelsius = CurrentWeather switch
            {
                "Sunny" => (byte)random.Next(15, 31),
                "Rainy" => (byte)random.Next(5, 21),
                _ => 20
            };
            _weatherChangeInterval = (byte)random.Next(5, 11);
        }

        public bool UpdateWeather(List<CarRaceData> cars)
        {
            _lapsSinceWeatherChange++;
            if (_lapsSinceWeatherChange >= _weatherChangeInterval)
            {
                GenerateWeather();

                double wearMultiplier = TyreWeather(TemperatureCelsius) / 100.0; 
                foreach (var carForeach in cars) 
                {
                    carForeach.SetMultipliers(this);
                }

                _lapsSinceWeatherChange = 0;
                return true;
            }
            return false;   
        }

        public static double TyreWeather(double temperature)
        {
            double optimalTemp = 25;
            double diff = Math.Abs(temperature - optimalTemp);
            int sign = Math.Sign(temperature - optimalTemp); // -1, 0 , 1
            return 100 + (2 + sign) * diff;  
        }   
    } 



 /*
    to do list:
    pit                                 | +
    check sim lap                       | +
    check ideal lap time                | +   
    UI | user interface                 | +
    AI | artificial intelligence        | 
    weather                             | + by tire
    accident dnf                        | +
    time list like per lap and general  | + 
    add wet tire                        | +
    tire bug fix                        | +

    ai
    ui
    main
    tech
*/



