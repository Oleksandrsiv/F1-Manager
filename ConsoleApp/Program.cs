using CarsLib;

class Program
{
    static void Main()
    {
        const int numberOfBots = 5;
        Console.Clear();
        IWeatherManager weatherManager = new WeatherManager ();
        Track track = new Track(weatherManager);
        Arrows arrows = new Arrows();

        Console.WriteLine("Welcome!");
        int totalLaps;
        while (true)
        {
            Console.Write("Enter count of laps: ");
            string input = Console.ReadLine();

            if (int.TryParse(input, out totalLaps) && totalLaps > 0)
                break;

            Console.WriteLine("Invalid input. Please enter a positive integer.");
        }

        Console.Write("Enter name of your team: ");
        string teamName = Console.ReadLine();

        CarF1 playerCar = new CarF1(teamName, 310, 100, 0.85, 100, 2, 100);
        CarRaceData playerData = new CarRaceData(playerCar);
        RaceManager raceManager = new RaceManager();

        string[] tireOptions = { "Soft", "Medium", "Hard", "Wet" };
        System.Console.WriteLine(weatherManager.CurrentWeather);
        int tireSelection = arrows.ShowArrowMenu("Choose type of tires:", tireOptions);
        byte tireInput = (byte)(tireSelection + 1);
        playerData.Car.SetTireType(tireInput);
        raceManager.RegisterCar(playerData);

        for (int i = 0; i < numberOfBots; i++)
        {
            CarF1 aiCar = CarFactory.CreateRandomizedCar($"AI Team {i + 1}");
            CarRaceData aiData = new CarRaceData(aiCar);
            raceManager.RegisterCar(aiData);
        }

        IRaceInterface raceInterface = new RaceInterface(playerData, raceManager, track, (WeatherManager)weatherManager, totalLaps);

        for (int lap = 1; lap <= totalLaps; lap++)
        {
            Console.Clear();

            if (playerCar.Dnf)
            {
                System.Console.WriteLine($"You did not finish!");
                raceInterface.ShowFinalResults();
            }
            raceInterface.AskPlayerPace();
            if (lap != 1)
            {
                raceInterface.OfferPitStop();
            }
            raceInterface.SimulateLap();
            raceInterface.ShowLapResults(lap, totalLaps);
            raceInterface.ShowRaceStatistics();

            System.Console.WriteLine("press any key to continue...");
            Console.ReadKey();
        }
        raceInterface.ShowFinalResults();

    }

        
}

