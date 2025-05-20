using CarsLib;

public class AIController
{

    public void AiMakeDecision(CarRaceData carData, Track track, WeatherManager weatherManager, int currentLap, int totalLaps)
    {
        var car = carData.Car;

        double distanceLeft = track.LengthKm * (totalLaps - currentLap);
        double fuelNeeded = distanceLeft * car.FuelConsumption1km;
        bool fuelEnoughToFinish = car.CurrentAmountOfFuel >= fuelNeeded;

        bool needTires = car.TireCondition < 25;
        bool badWeather = weatherManager.CurrentWeather != "Sunny";
        bool isWetWeather = weatherManager.CurrentWeather == "Rainy";

        bool tireWeatherMismatch =
            (car.TypeOfTire <= 3 && isWetWeather) ||     // dry tires in wet
            (car.TypeOfTire == 4 && !isWetWeather);      // wet tires in dry

        bool finalLaps = (totalLaps - currentLap) <= 2;

        // Decide Pace
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
            // Calculate refuel amount
            double refuelAmount = Math.Min(
                car.MaxVolumeOfTank - car.CurrentAmountOfFuel,
                Math.Max(track.LengthKm * 3 * car.FuelConsumption1km, fuelNeeded)
            );

            int remainingLaps = totalLaps - currentLap;
            byte optimalTire = ChooseOptimalTireType(weatherManager.CurrentWeather, remainingLaps, track.LengthKm);

            byte? newTireType = null;

            // We change the rubber if it does not meet the optimal condition or if replacement is required due to wear.
            if (car.TypeOfTire != optimalTire || needTires)
            {
                newTireType = optimalTire;
            }

            car.PitStop(refuelAmount, weatherManager, newTireType);

        }
    }


    private byte ChooseOptimalTireType(string weather, int remainingLaps, double trackLengthKm)
    {
        if (weather == "Rainy")
            return 4; // Wet

        if (remainingLaps <= 2)
            return 1; // Soft

        double remainingDistance = remainingLaps * trackLengthKm;

        if (remainingDistance < 20)
            return 1; // Soft
        else if (remainingDistance < 40)
            return 2; // Medium
        else
            return 3; // Hard
    }

}

