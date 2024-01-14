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
using System.IO;
using System.Windows.Forms;


namespace CustomRPC_Service
{
    public partial class CustomRPC : ServiceBase
    {
        const string CLIENT_ID = "1115004659384471714";
        static Discord.Discord discord;

        static Discord.Activity activity;
        private static System.Timers.Timer timer = new System.Timers.Timer();

        public void WriteToFile(string Message)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(filepath))
            {
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }

        }

        public CustomRPC()
        {
            InitializeComponent();
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 5000;
            timer.Enabled = true;
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToFile("Process recalled at " + DateTime.Now.ToString());
            discord.RunCallbacks();
        }
        protected override void OnStart(string[] args)
        {
            WriteToFile("Service started at " + DateTime.Now.ToString());
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
                    LargeImage = "jim-carrey-typing-2400596192", // Larger Image Asset Value
                    LargeText = "Work", // Large Image Tooltip
                    SmallImage = "certified", // Small Image Asset Value
                    SmallText = "Certified Hard Worker", // Small Image Tooltip
                },
            };


            discord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                WriteToFile("Log[{0}] {1} " + level + message);
            });

            discord.RunCallbacks();
            UpdateActivity(discord, activity);
            discord.RunCallbacks();
        }

        protected override void OnStop()
        {
            WriteToFile("Service stopped at " + DateTime.Now.ToString());
            discord.RunCallbacks();
            discord.Dispose();
        }


        static long GetEndTime()
        {
            //Current unix time
            long timestamp = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            DateTime dt = DateTime.Now;

            if (dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday)
            {
                TimeSpan dayShift = new TimeSpan(7, 30, 0);
                TimeSpan nightShift = new TimeSpan(19, 30, 0);
                TimeSpan now = DateTime.Now.TimeOfDay;

                if (nightShift < now)
                {
                    //If current time is between 7:30pm and 7:30am

                    timestamp = timestamp + (long)((dayShift.TotalSeconds - now.Subtract(TimeSpan.FromHours(24)).TotalSeconds));
                    return timestamp;
                }
                else if (dayShift < now)
                {
                    //If current time is between 11:30pm and 7:30am
                    timestamp = timestamp + (long)((dayShift.TotalSeconds - now.Subtract(TimeSpan.FromHours(12)).TotalSeconds));
                    return timestamp;
                }

                return timestamp;

            }
            else
            {

                TimeSpan morningShift = new TimeSpan(7, 30, 0);
                TimeSpan eveningShift = new TimeSpan(15, 30, 0);
                TimeSpan nightShift = new TimeSpan(23, 30, 0);
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
                else if ((nightShift < now))
                {
                    //If current time is between 11:30pm and 7:30am

                    timestamp = timestamp + (long)((morningShift.TotalSeconds - now.Subtract(TimeSpan.FromHours(24)).TotalSeconds));
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


        }

        static void UpdateActivity(Discord.Discord discord, Discord.Activity activity)
        {
            activity.Timestamps.End = GetEndTime();
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

    }
}
