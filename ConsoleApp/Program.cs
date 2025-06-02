using System.Text;
using RaceLib.Car;
using RaceLib.UI;

class Program
{
    static void Main()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        const int numberOfBots = 5;
        Console.Clear();

        IWeatherManager weatherManager = new WeatherManager();
        Track track = new Track(weatherManager);
        Arrows arrows = new Arrows();

        Console.WriteLine("Welcome to F1 Race!");

        int totalLaps = GetNumberFromMenu("Select number of laps:", new[] { "5", "10", "15","20"});
        Console.Write("Enter name of your team: ");
        string teamName = Console.ReadLine();

        CarF1 playerCar = new CarF1(teamName, 310, 100, 0.85, 100, 2, 100);
        CarRaceData playerData = new CarRaceData(playerCar);
        RaceManager raceManager = new RaceManager();

        string[] tireOptions = { "Soft", "Medium", "Hard", "Wet" };
        Console.WriteLine($"Weather: {weatherManager.CurrentWeather}");
        int tireSelection = arrows.ShowArrowMenu($"Weather: {weatherManager.CurrentWeather}\nChoose tire type:", tireOptions);
        playerData.Car.SetTireType((byte)(tireSelection + 1));
        raceManager.RegisterCar(playerData);

        for (int i = 0; i < numberOfBots; i++)
        {
            var ai = CarFactory.CreateRandomizedCar($"AI Team {i + 1}");
            raceManager.RegisterCar(new CarRaceData(ai));
        }

        IRaceInterface raceInterface = new RaceInterface(playerData, raceManager, track, (WeatherManager)weatherManager, totalLaps);

        for (int lap = 1; lap <= totalLaps; lap++)
        {
            Console.Clear();

            if (playerCar.Dnf)
            {
                Console.WriteLine("You did not finish!");
                raceInterface.ShowFinalResults();
                return;
            }

            raceInterface.AskPlayerPace();
            if (lap != 1)
                raceInterface.OfferPitStop();

            raceInterface.SimulateLap();
            raceInterface.ShowLapResults(lap, totalLaps);
            raceInterface.ShowRaceStatistics();

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        raceInterface.ShowFinalResults();
    }

    static int GetNumberFromMenu(string title, string[] options)
    {
        Arrows arrows = new Arrows();
        int selectedIndex = arrows.ShowArrowMenu(title, options);
        return int.Parse(options[selectedIndex]);
    }
}
