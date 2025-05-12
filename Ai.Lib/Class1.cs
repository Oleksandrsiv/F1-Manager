namespace CarsLib;

using System;
using CarsLib;

public class AIController
{
    private int totalLaps;

    public AIController(int totalLaps)
    {
        this.totalLaps = totalLaps;
    }

    public void AiMakeDecision(CarRaceData carData, Track track, WeatherManager weatherManager, int currentLap, int totalLaps)
    {
    var car = carData.Car;

    double distanceLeft = track.LengthKm * (totalLaps - currentLap);
    double fuelNeeded = distanceLeft * car.FuelConsumption1km;
    bool fuelEnoughToFinish = car.CurrentAmountOfFuel >= fuelNeeded;

    //bool needRefuel = !fuelEnoughToFinish;
    bool needTires = car.TireCondition < 25;

    bool badWeather = weatherManager.CurrentWeather != "Sunny";

    bool tireWeatherMismatch =
        (car.TypeOfTire <= 3 && badWeather) ||     // dry tires in bad weather
        (car.TypeOfTire == 4 && !badWeather);      // wet tires in sunny

    bool finalLaps = totalLaps - currentLap <= 2;

    //  Decide Pace 
    if (car.TireCondition > 70 && car.CurrentAmountOfFuel > track.LengthKm * 2 && !badWeather)
    {
        car.Pace = 3; // aggressive
    }
    else if (car.TireCondition < 40 || car.CurrentAmountOfFuel < track.LengthKm * 1.5)
    {
        car.Pace = 1; // economic
    }
    else
    {
        car.Pace = 2; // normal
    }

    // Decide Pit Stop 
    bool needPitStop = 
        (!fuelEnoughToFinish && !finalLaps) ||
        (needTires && !finalLaps) ||
        (tireWeatherMismatch && !finalLaps);

    if (needPitStop)
    {
        //refill if needed
        double refuelAmount = Math.Min(
            car.MaxVolumeOfTank - car.CurrentAmountOfFuel,
            Math.Max(track.LengthKm * 3 * car.FuelConsumption1km, fuelNeeded)
        );

        byte? newTireType = null;

        if (weatherManager.CurrentWeather == "Rainy")
        {
            newTireType = 4; // wet
        }
        else if (car.TypeOfTire == 4)
        {
            newTireType = 2; // switch from wet to medium
        }
        else if (needTires)
        {
            newTireType = car.TypeOfTire; // same type, just fresher
        }

        car.PitStop(refuelAmount, weatherManager, newTireType);
    }
    }

}
