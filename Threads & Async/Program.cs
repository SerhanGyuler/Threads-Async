using System;
using System.Threading;
using System.Collections.Generic;

namespace Threads___Async
{
    internal class Program
    {
        public static readonly Mutex mutex = new Mutex();
        public static bool winnerDeclared = false;

        static void Main(string[] args)
        {
            // Create two car objects with names
            Car car1 = new Car("Dom");
            Car car2 = new Car("Brian");

            // Add cars to a list
            List<Car> cars = new List<Car> { car1, car2 };

            // Create and start threads for each car 
            Thread carThread1 = new Thread(() => car1.Drive());
            Thread carThread2 = new Thread(() => car2.Drive());
            // Create and start a thread for the status listener
            Thread statusThread = new Thread(() => StatusListener(cars));

            Console.WriteLine("Startar biltävlingen!\n");

            // Start threads
            statusThread.Start();
            carThread1.Start();
            carThread2.Start();

            // Wait for both car threads to finish
            carThread1.Join();
            carThread2.Join();

            // Stop SL after race ends
            statusThread.Interrupt();

            Console.WriteLine("\nTävlingen är över!");
        }

        // Method to listen for status requests from the user
        static void StatusListener(List<Car> cars)
        {
            while (true)
            {
                try
                {
                    string input = Console.ReadLine();
                    if (input == "" || input?.ToLower() == "status")
                    {
                        // Lock shared resource to avoid concurrent output
                        mutex.WaitOne();
                        Console.WriteLine("\nSTATUSUPPDATERING:");
                        foreach (Car car in cars)
                        {
                            Console.WriteLine($"{car.Name} - {car.DistanceDriven:F2} km, {car.Speed:F1} km/h");
                        }
                        Console.WriteLine();
                        mutex.ReleaseMutex();
                    }
                }
                catch (ThreadInterruptedException)
                {
                    break;
                }
            }
        }
    }

    class Car
    {
        public string Name { get; }
        public double DistanceDriven { get; private set; } = 0; // Dustance default 0
        public double Speed { get; private set; } = 120.0; // km/h
        public const double RaceLength = 5.0; // km
        public Random rng = new Random(Guid.NewGuid().GetHashCode()); // Random generator

        // Constructor that initializes the car with a name
        public Car(string name)
        {
            Name = name;
        }

        public void Drive()
        {
            Console.WriteLine($"{Name} har börjat köra!");

            int secondsPassed = 0;

            // Main loop for race
            while (DistanceDriven < RaceLength)
            {
                Thread.Sleep(1000); // 1 second per loop
                DistanceDriven += Speed / 3600.0; // km/h to km per second
                secondsPassed++;

                // Every 10 seconds, check if any event occurs
                if (secondsPassed % 10 == 0)
                {
                    CheckForEvent();
                }

                // Lock shared resource to prevent concurrent console output
                Program.mutex.WaitOne();
                Console.WriteLine($"{Name} har kört {DistanceDriven:F2} km");
                Program.mutex.ReleaseMutex();
            }

            // When the car reaches the finish line, check if it’s the winner
            Program.mutex.WaitOne();
            if (!Program.winnerDeclared)
            {
                Program.winnerDeclared = true;
                Console.WriteLine($"{Name} har gått i mål och VINNER TÄVLINGEN!");
            }
            else
            {
                Console.WriteLine($"{Name} har gått i mål!");
            }
            Program.mutex.ReleaseMutex();
        }

        // Method to check for random events every 10 seconds
        private void CheckForEvent()
        {
            int chance = rng.Next(1, 51); // 1 till 50

            if (chance == 1)
            {
                StopForEvent("slut på bensin", 15);
            }
            else if (chance <= 3)
            {
                StopForEvent("punktering", 10);
            }
            else if (chance <= 8)
            {
                StopForEvent("fågel på vindrutan", 5);
            }
            else if (chance <= 18)
            {
                Speed -= 1;
                // Lock console output to safely print the motor issue message
                Program.mutex.WaitOne();
                Console.WriteLine($"{Name} har motorproblem! Hastigheten sänks till {Speed:F1} km/h");
                Program.mutex.ReleaseMutex();
            }
        }

        // Method to stop the car for a specific event
        private void StopForEvent(string reason, int seconds)
        {
            Program.mutex.WaitOne();
            Console.WriteLine($"{Name} stannar p.g.a. {reason}. Väntar i {seconds} sekunder...");
            Program.mutex.ReleaseMutex();

            Thread.Sleep(seconds * 1000); // Simulate the stop time due to the event
        }
    }
}
