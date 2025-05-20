using CarsLib;



class Program
{
    static void Main()
    {   
        Console.InputEncoding = System.Text.Encoding.UTF8;
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        WeatherManager weatherManager = new WeatherManager();
        Track track = new Track(weatherManager);

        Console.WriteLine("Welcome!");
        Console.Write("Enter count of laps: ");
        string input = Console.ReadLine();
        int totalLaps;

        if (!int.TryParse(input, out totalLaps) || totalLaps <= 0)
        {
            Console.WriteLine("incorrect value. 5 laps have been set by default..");
            totalLaps = 5;
            
        } 

        Console.Write("Enter name of your team: ");
        string teamName = Console.ReadLine();

        CarF1 playerCar = new CarF1(teamName, 310, 0, 2, 100, 0.85, 90, 2, 100);
        CarRaceData playerData = new CarRaceData(playerCar);
        RaceManager raceManager = new RaceManager();

        bool isValid = false;
        byte tireInput;

        while (!isValid)
        {
            Console.WriteLine($"\nChoose type of tires:\n1. Soft\n2. Medium\n3. Hard\n4. Wet");
            string tryInput = Console.ReadLine();

            if (byte.TryParse(tryInput, out tireInput))
            {
                playerData.Car.SetTireType(tireInput , out isValid);
            }
        }


        raceManager.RegisterCar(playerData);
        for (int i = 0; i < 5; i++)
        {
            CarF1 aiCar = CarFactory.CreateRandomizedCar($"AI Team {i + 1}");
            CarRaceData aiData = new CarRaceData(aiCar);
            raceManager.RegisterCar(aiData);
        }

        IRaceInterface raceInterface = new RaceInterface(playerData, raceManager, track, weatherManager, totalLaps);


        for (int lap = 1; lap <= totalLaps; lap++)
        {
            Console.Clear();
            Console.WriteLine($"\nlap {lap}/{totalLaps}");
            Console.WriteLine(new string('-', 50));

            if (playerCar.Dnf)
            {
                System.Console.WriteLine($"You did not finish! \nRace is over!");
                raceInterface.ShowFinalResults();
            }

            raceInterface.AskPlayerPace();
            raceInterface.OfferPitStop();
            raceInterface.SimulateLap();
            raceInterface.ShowLapResults();
            raceInterface.ShowRaceStatistics();

            System.Console.WriteLine("press any key to continue...");
            Console.ReadKey();
        }

        raceInterface.ShowFinalResults();
    }
}
