using System;
using System.Diagnostics;
using System.Management;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;

class Program
{
    static PerformanceCounter ramCounter = new PerformanceCounter("Memory", "Available MBytes");

    static void Main()
    {
        Console.WriteLine("Surveillance des processus, services et RAM en cours...");

        // Surveillance des processus
        WatchProcesses();

        // Surveillance des services
        WatchServices();

        Console.WriteLine("Appuyez sur Entrée pour quitter.");
        Console.ReadLine();
    }

    static void WatchProcesses()
    {
        // Détecter les nouveaux processus lancés
        ManagementEventWatcher startWatcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
        startWatcher.EventArrived += (sender, e) =>
        {
            string processName = e.NewEvent["ProcessName"]?.ToString();
            Console.WriteLine($"[Processus] Démarré : {processName} | RAM dispo : {GetAvailableRAM()} MB");
        };
        startWatcher.Start();

        // Détecter les processus arrêtés
        ManagementEventWatcher stopWatcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace"));
        stopWatcher.EventArrived += (sender, e) =>
        {
            string processName = e.NewEvent["ProcessName"]?.ToString();
            Console.WriteLine($"[Processus] Arrêté : {processName} | RAM dispo : {GetAvailableRAM()} MB");
        };
        stopWatcher.Start();
    }

    static void WatchServices()
    {
        List<string> knownServices = ServiceController.GetServices().Select(s => s.ServiceName).ToList();

        // Vérifier toutes les 5 secondes si des services ont changé d'état
        System.Timers.Timer timer = new System.Timers.Timer(5000);
        timer.Elapsed += (sender, e) =>
        {
            var currentServices = ServiceController.GetServices().Select(s => s.ServiceName).ToList();

            // Détecter les nouveaux services démarrés
            var newServices = currentServices.Except(knownServices).ToList();
            foreach (var service in newServices)
            {
                Console.WriteLine($"[Service] Démarré : {service} | RAM dispo : {GetAvailableRAM()} MB");
            }

            // Détecter les services arrêtés
            var stoppedServices = knownServices.Except(currentServices).ToList();
            foreach (var service in stoppedServices)
            {
                Console.WriteLine($"[Service] Arrêté : {service} | RAM dispo : {GetAvailableRAM()} MB");
            }

            // Mettre à jour la liste des services connus
            knownServices = currentServices;
        };

        timer.Start();
    }

    static float GetAvailableRAM()
    {
        return ramCounter.NextValue(); // Retourne la RAM disponible en MB
    }
}
