using System;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static void Main()
    {
        Console.WriteLine("Surveillance des processus et services en cours...");

        // Surveillance des processus
        WatchProcesses();

        // Surveillance des services
        WatchServices();

        Console.WriteLine("Appuyez sur Entrée pour quitter.");
        Console.ReadLine();
    }

    static void WatchProcesses()
    {
        ManagementEventWatcher startWatcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));

        startWatcher.EventArrived += (sender, e) =>
        {
            string processName = e.NewEvent["ProcessName"]?.ToString();
            Console.WriteLine($"[Processus] Nouveau processus démarré : {processName}");
        };

        startWatcher.Start();
    }

    static void WatchServices()
    {
        List<string> knownServices = ServiceController.GetServices().Select(s => s.ServiceName).ToList();

        // Vérifier toutes les 5 secondes les nouveaux services
        System.Timers.Timer timer = new System.Timers.Timer(5000);
        timer.Elapsed += (sender, e) =>
        {
            var currentServices = ServiceController.GetServices().Select(s => s.ServiceName).ToList();

            var newServices = currentServices.Except(knownServices).ToList();
            foreach (var service in newServices)
            {
                Console.WriteLine($"[Service] Nouveau service détecté : {service}");
            }

            knownServices = currentServices;
        };

        timer.Start();
    }
}