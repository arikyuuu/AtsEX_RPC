using System;
using System.Runtime.InteropServices;

using AtsEx.PluginHost;
using AtsEx.PluginHost.Plugins;
using AtsEx.PluginHost.Plugins.Extensions;
using AtsEx.PluginHost.Native;
using BveTypes.ClassWrappers;

using DiscordRPC;
using DiscordRPC.Logging;

namespace AtsEX_RPC
{
    [Plugin(PluginType.Extension)]
    [Togglable]
    internal class ExtensionMain : AssemblyPluginBase, ITogglableExtension, IExtension
    {
        private bool status = true;
        public DiscordRpcClient Client { get; private set; }

        public bool IsEnabled
        {
            get { return status; }
            set { status = value; }
        }

        [DllImport("kernel32")]
        static extern bool AllocConsole();

        public ExtensionMain(PluginBuilder builder) : base(builder)
        {
            //debug console
			//AllocConsole();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Client = new DiscordRpcClient("000000000000000");
            Client.Logger = new ConsoleLogger() { Level = LogLevel.Warning };
            Client.OnReady += (sender, e) =>
            {
                Console.WriteLine("Received Ready from user {0}", e.User.Username);
            };

            Client.OnPresenceUpdate += (sender, e) =>
            {
                Console.WriteLine("Received Update! {0}", e.Presence);
            };
            Client.Initialize();
            //Todo implement scenario closed
        }

        public override void Dispose()
        {
            Client.Dispose();
        }

        public static string ToTimeText(int timeMilliseconds)
        {
            int timeSeconds = timeMilliseconds / 1000;

            int hours = timeSeconds / 3600;
            int minutes = timeSeconds / 60 % 60;
            int seconds = timeSeconds % 60;

            return $"{hours}:{minutes:D2}:{seconds:D2}";
        }


        private void updatePresence()
        {
            Console.WriteLine("------------ PRESENCE START");
            IBveHacker bveHacker = BveHacker;
            Scenario scenario = bveHacker.Scenario;
            string RPCstate;
            string RPCdetails;

            if (scenario is null) {
                RPCstate = "Idle";
                RPCdetails = "Main Menu";
            } else
            {
                try
                {
                    string mapName = bveHacker.ScenarioInfo.RouteTitle;
                    string vehicleName = bveHacker.ScenarioInfo.VehicleTitle;

                    RPCdetails = mapName + ", " + vehicleName;

                    double speed = scenario.LocationManager.SpeedMeterPerSecond * 3.6;
                    
                    int timems = scenario.TimeManager.TimeMilliseconds;
                    string time = ToTimeText(timems);

                    StationList stations = scenario.Route.Stations;
                    int stationIndex = stations.CurrentIndex;
                    Station confirmedStation = scenario.Vehicle.Conductor.Stations.Count <= stationIndex ? null : scenario.Vehicle.Conductor.Stations[stationIndex] as Station;
                    var nextStaIndex = stations.CurrentIndex + 1;
                    Station nextStation = stations.Count <= nextStaIndex ? null : stations[nextStaIndex] as Station;
                    Station lastStation = stations.Count == 0 ? null : stations[stations.Count - 1] as Station;

                    Console.WriteLine((int)speed + "km/h");
                    Console.WriteLine(time);
                    Console.WriteLine(confirmedStation.Name);

                    int nextStationArrivalTime;
                    string nextStationName;
                    if(nextStation != null)
                    {
                        Console.WriteLine("next station");
                        Console.WriteLine(nextStation.Name);
                        nextStationName = nextStation.Name;
                        nextStationArrivalTime = nextStation.ArrivalTimeMilliseconds;
                    } else
                    { //WE ARE AT THE END!
                        Console.WriteLine("final station");
                        Console.WriteLine(lastStation.Name);
                        if (confirmedStation.Name == lastStation.Name)
                        {
                            nextStationName = confirmedStation.Name;
                            nextStationArrivalTime = -2; //finished route
                        }
                        else
                        {
                            nextStationName = lastStation.Name;
                            nextStationArrivalTime = lastStation.ArrivalTimeMilliseconds;
                        }
                    }

                    Console.WriteLine("#####------####");
                    Console.WriteLine(nextStationArrivalTime);
                    Console.WriteLine(timems);
                    Console.WriteLine("#####------######");
                    string onTime = "On Time";
                    if(nextStationArrivalTime == -2)
                    {
                        Console.WriteLine("Finished route");
                        onTime = "Final St.";
                    } else if(nextStationArrivalTime == -1)
                    {
                        Console.WriteLine("On time");
                        onTime = "On Time";
                    } else
                    {
                        if (timems >= nextStationArrivalTime)
                        {
                            Console.WriteLine("LATE!");
                            onTime = "Delayed";
                        }
                        else
                        {
                            Console.WriteLine("on Time");
                            onTime = "On Time";
                        }
                    }
                    Console.WriteLine("----------- PRESENCE END");
                    RPCstate = (int)speed + "km/h ("+onTime+") | "+nextStationName + " st.";
                } catch(Exception ex)
                {
                    RPCstate = "Idle";
                    RPCdetails = "Main menu";
                    Console.WriteLine("Error "+ex.Message);
                }
            }

            Client.SetPresence(new RichPresence()
            {

                Details = RPCdetails,
                Timestamps = Timestamps.Now,
                State = RPCstate,
                Assets = new Assets()
                {
                    LargeImageKey = "yea",
                    LargeImageText = "null",
                }
            }); ;
        }

        private TimeSpan _timeSinceLastExecution = TimeSpan.Zero;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(10);

        public override TickResult Tick(TimeSpan elapsed)
        {
            if (elapsed.TotalSeconds < 5)
            {
                _timeSinceLastExecution += elapsed;
            } else
            {
                Console.WriteLine("TIME MOVED TOO FAST!");
            }

            if (_timeSinceLastExecution >= _interval)
            {
                _timeSinceLastExecution = TimeSpan.Zero;
                updatePresence();
                Console.WriteLine("Executing action after 30 seconds...");
            }
            return new ExtensionTickResult();
        }
    }
}