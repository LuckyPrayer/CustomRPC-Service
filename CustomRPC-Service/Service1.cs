using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Management;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Discord;
using System.Threading;
using System.Timers;

namespace CustomRPC_Service
{
    public partial class Service1 : ServiceBase
    {
        //--------Window Visabilty---------
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        const string CLIENT_ID = "1115004659384471714";
        static Discord.Discord discord;

        static Discord.Activity activity;
        private static System.Timers.Timer timer = new System.Timers.Timer();
        public void debug()
        {
            OnStart(null);
        }

        public Service1()
        {

            InitializeComponent();

            while (Process.GetProcessesByName("Discord").Length == 0)
            {

            }
            System.Threading.Thread.Sleep(5000);
            discord = new Discord.Discord(Int64.Parse(CLIENT_ID), (UInt64)Discord.CreateFlags.NoRequireDiscord);
            activity = new Discord.Activity
            {
                //https://discord.com/developers/docs/game-sdk/activities
                //https://discord.com/developers/applications/1115004659384471714/rich-presence/visualizer
                Name = "Work",
                State = "Hardly working",
                Details = "Hard at work",
                Timestamps =
                {
                    End = 1702643400,
                },
                Assets =
                {
                    LargeImage = "desk-clipart-desk-job-12-4168606885", // Larger Image Asset Value
                    LargeText = "Work", // Large Image Tooltip
                    SmallImage = "foo smallImageKey", // Small Image Asset Value
                    SmallText = "foo smallImageText", // Small Image Tooltip
                },
            };

            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_HIDE);




            activity.Timestamps.End = GetEndTime();

            discord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                Console.WriteLine("Log[{0}] {1}", level, message);
            });

            discord.RunCallbacks();

            Process[] processes = Process.GetProcessesByName("CDViewer");
            if (processes.Length != 0)
            {
                Console.WriteLine("CDViewer detected");
                UpdateActivity(discord, activity);
                System.Threading.Thread.Sleep(3000);
                discord.RunCallbacks();
            }


            string process = "CDViewer";
            WatchForProcessStart(process);
            WatchForProcessEnd(process);

            /*
            while (true)
            {
                discord.RunCallbacks();
            }
            */
        }
        private void OnElaspedTime(object source, ElapsedEventArgs e)
        {
            discord.RunCallbacks();
        }
        protected override void OnStart(string[] args)
        {
            timer.Elapsed += new System.Timers.ElapsedEventHandler(OnElaspedTime);
            timer.Interval = 5000;
            timer.Enabled = true;
        }

        protected override void OnStop()
        {
            discord.Dispose();
        }



        static long GetEndTime()
        {
            //Current unix time
            long timestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

            TimeSpan morningShift = new TimeSpan(7, 30, 0); //10 o'clock
            TimeSpan eveningShift = new TimeSpan(15, 30, 0); //12 o'clock
            TimeSpan nightShift = new TimeSpan(23, 30, 0); //12 o'clock
            TimeSpan now = DateTime.Now.TimeOfDay;


            if ((morningShift < now) && (now < eveningShift))
            {
                //If current time is between 7:30am and 3:30pm

                timestamp = timestamp + (long)((eveningShift.Subtract(now)).TotalSeconds);
                return timestamp;

            }
            else if ((eveningShift < now) && (now < nightShift))
            {
                //If current time is between 330pm and 11:30pm

                timestamp = timestamp + (long)((nightShift.Subtract(now)).TotalSeconds);
                return timestamp;
            }
            else if ((nightShift < now) && (now < morningShift))
            {
                //If current time is between 11:30pm and 7:30am

                timestamp = timestamp + (long)((morningShift.Subtract(now)).TotalSeconds);
                return timestamp;
            }
            else if (now < morningShift)
            {
                //If current time is between 11:30pm and 7:30am

                timestamp = timestamp + (long)((morningShift.Subtract(now)).TotalSeconds);
                return timestamp;
            }

            return timestamp;
        }

        static void UpdateActivity(Discord.Discord discord, Discord.Activity activity)
        {
            var activityManager = discord.GetActivityManager();
            activityManager.UpdateActivity(activity, (result) =>
            {
                if (result == Discord.Result.Ok)
                {
                    Console.WriteLine("RPC Update Success!");
                }
                else
                {
                    Console.WriteLine("RPC Update Failed");
                }
            });


        }

        static void ClearActivity(Discord.Discord discord)
        {
            var activityManager = discord.GetActivityManager();
            activityManager.ClearActivity((result) =>
            {
                if (result == Discord.Result.Ok)
                {
                    Console.WriteLine("RPC Clear Success!");
                }
                else
                {
                    Console.WriteLine("RPC Clear Failed");
                }
            });
  

        }



        static ManagementEventWatcher WatchForProcessStart(string processName)
        {
            string queryString =
                "SELECT TargetInstance" +
                "  FROM __InstanceCreationEvent " +
                "WITHIN  10 " +
                " WHERE TargetInstance ISA 'Win32_Process' " +
                "   AND TargetInstance.Name = '" + processName + "'";

            // The dot in the scope means use the current machine
            string scope = @"\\.\root\CIMV2";

            // Create a watcher and listen for events
            ManagementEventWatcher watcher = new ManagementEventWatcher(scope, queryString);
            watcher.EventArrived += ProcessStarted;
            watcher.Start();
            return watcher;
        }



        static ManagementEventWatcher WatchForProcessEnd(string processName)
        {
            string queryString =
                "SELECT TargetInstance" +
                "  FROM __InstanceDeletionEvent " +
                "WITHIN  10 " +
                " WHERE TargetInstance ISA 'Win32_Process' " +
                "   AND TargetInstance.Name = '" + processName + "'";

            // The dot in the scope means use the current machine
            string scope = @"\\.\root\CIMV2";

            // Create a watcher and listen for events
            ManagementEventWatcher watcher = new ManagementEventWatcher(scope, queryString);
            watcher.EventArrived += ProcessEnded;
            watcher.Start();
            return watcher;
        }


        static protected void ProcessEnded(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            string processName = targetInstance.Properties["Name"].Value.ToString();
            Console.WriteLine(String.Format("{0} process ended", processName));
            ClearActivity(discord);
            System.Threading.Thread.Sleep(3000);
        }


        static protected void ProcessStarted(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;
            string processName = targetInstance.Properties["Name"].Value.ToString();
            Console.WriteLine(String.Format("{0} process started", processName));
            UpdateActivity(discord, activity);
            System.Threading.Thread.Sleep(3000);
            discord.RunCallbacks();
        }



    }
}
