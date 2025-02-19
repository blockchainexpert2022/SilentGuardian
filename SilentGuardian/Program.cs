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
        ManagementEventWatcher startWatcher = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
        startWatcher.EventArrived += (sender, e) =>
        {
            string processName = e.NewEvent["ProcessName"]?.ToString();
            int processId = Convert.ToInt32(e.NewEvent["ProcessID"]);
            string owner = GetProcessOwner(processId);
            Console.WriteLine($"[Processus] Démarré : {processName} (Utilisateur : {owner}) | RAM dispo : {GetAvailableRAM()} MB");
        };
        startWatcher.Start();

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
        Dictionary<string, ServiceControllerStatus> knownServices = ServiceController.GetServices()
            .ToDictionary(s => s.ServiceName, s => s.Status);

        System.Timers.Timer timer = new System.Timers.Timer(5000);
        timer.Elapsed += (sender, e) =>
        {
            var currentServices = ServiceController.GetServices()
                .ToDictionary(s => s.ServiceName, s => s.Status);

            foreach (var service in currentServices)
            {
                if (knownServices.ContainsKey(service.Key) && knownServices[service.Key] != ServiceControllerStatus.Running && service.Value == ServiceControllerStatus.Running)
                {
                    string user = GetServiceUser(service.Key);
                    Console.WriteLine($"[Service] Démarré : {service.Key} (Utilisateur : {user}) | RAM dispo : {GetAvailableRAM()} MB");
                }
            }

            foreach (var service in knownServices)
            {
                if (currentServices.ContainsKey(service.Key) && service.Value == ServiceControllerStatus.Running && currentServices[service.Key] != ServiceControllerStatus.Running)
                {
                    Console.WriteLine($"[Service] Arrêté : {service.Key} | RAM dispo : {GetAvailableRAM()} MB");
                }
            }

            knownServices = currentServices;
        };

        timer.Start();
    }

    static string GetProcessOwner(int processId)
    {
        try
        {
            string query = $"SELECT * FROM Win32_Process WHERE ProcessId = {processId}";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    object[] ownerInfo = new object[2];
                    obj.InvokeMethod("GetOwner", ownerInfo);
                    return ownerInfo[0]?.ToString() ?? "Inconnu";
                }
            }
        }
        catch (Exception)
        {
            return "Inconnu";
        }
        return "Inconnu";
    }

    static string GetServiceUser(string serviceName)
    {
        try
        {
            string query = $"SELECT StartName FROM Win32_Service WHERE Name = '{serviceName}'";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["StartName"]?.ToString() ?? "Système";
                }
            }
        }
        catch (Exception)
        {
            return "Inconnu";
        }
        return "Inconnu";
    }

    static float GetAvailableRAM()
    {
        return ramCounter.NextValue();
    }
}
