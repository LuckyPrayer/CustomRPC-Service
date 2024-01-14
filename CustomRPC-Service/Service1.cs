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
using System.IO.Ports;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.Net.NetworkInformation;


namespace CustomRPC_Service
{
    public partial class CustomRPC : ServiceBase
    {
        const string CLIENT_ID = "1115004659384471714";
        static Discord.Discord discord;

        Discord.Activity activity;
        private static System.Timers.Timer timer = new System.Timers.Timer();
        string configPath = AppDomain.CurrentDomain.BaseDirectory;
        string logPath = @"C:\Logs\CustomRPC";
        public void WriteToLog(string Message)
        {
            
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            string logFilePath = logPath + @"\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".txt";
            if (!File.Exists(logFilePath))
            {
                using (StreamWriter sw = File.CreateText(logFilePath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(logFilePath))
                {
                    sw.WriteLine(Message);
                }
            }

        }
        public void CreateConfig()
        {
            //string path = AppDomain.CurrentDomain.BaseDirectory + "\\Config";
            
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            string configFilePath = configPath + @"\config.txt";
            using (StreamWriter sw = File.CreateText(configFilePath))
            {
                sw.WriteLine("Hardly working"); //Details
                sw.WriteLine("Hard at work"); //
                sw.WriteLine("1"); //Timestamps Start
                sw.WriteLine("1"); //Timestamps End
                sw.WriteLine("typing"); //Assets LargeImage
                sw.WriteLine("Work"); //Assets 
                sw.WriteLine("certified"); //Assets 
                sw.WriteLine("Certified Hard Worker"); //Assets 
                sw.WriteLine("00"); //Party ID 
                sw.WriteLine("0"); //Party Size
                sw.WriteLine("0"); //Party Max Size
      
            }
        }

        public void ReadConfig()
        {

            string configFilePath = configPath + @"config.txt";
            //Check if config file exists
            if (!File.Exists(configFilePath))
            {
                //Create config file
                CreateConfig();
            }

            StreamReader reader = new StreamReader(configFilePath);

            string actState = reader.ReadLine();
            string actDetails = reader.ReadLine();
            string timestampStart = reader.ReadLine();
            string timestampEnd = reader.ReadLine();
            string actLargeImage = reader.ReadLine();
            string actLargeText = reader.ReadLine();
            string actSmallImage = reader.ReadLine();
            string actSmallText = reader.ReadLine();
            string actID = reader.ReadLine();
            Int32 actCurrentSize = Convert.ToInt32(reader.ReadLine());
            Int32 actMaxSize = Convert.ToInt32(reader.ReadLine());

            activity = new Discord.Activity
            {
                State = actState,
                Details = actDetails,
                Timestamps =
                    {
                        Start = Convert.ToInt32(timestampStart),
                        End = Convert.ToInt32(timestampEnd),
                    },
                Assets =
                    {
                        LargeImage = actLargeImage, // Larger Image Asset Value
                        LargeText = actLargeText, // Large Image Tooltip
                        SmallImage = actSmallImage, // Small Image Asset Value
                        SmallText = actSmallText, // Small Image Tooltip
                    },
                Party =
                    {
                        Id = actID,
                        Size =
                        {
                            CurrentSize = actCurrentSize,
                            MaxSize = actMaxSize,
                        },
                    },
            };
            reader.Close();

        }

        public CustomRPC()
        {
            InitializeComponent();
            discord = new Discord.Discord(Int64.Parse(CLIENT_ID), (UInt64)Discord.CreateFlags.NoRequireDiscord);

            WriteToLog("Process initalized at " + DateTime.Now.ToString());
            timer.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timer.Interval = 3000;
            timer.Enabled = true;
        }
        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            WriteToLog("Process recalled at " + DateTime.Now.ToString());
            discord.RunCallbacks();
        }
        protected override void OnStart(string[] args)
        {
            //System.Diagnostics.Debugger.Launch();
            WriteToLog("Service started at " + DateTime.Now.ToString());
            ReadConfig();

            discord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                WriteToLog("Log[{0}] {1} " + level + message);
            });

            discord.RunCallbacks();
            UpdateActivity(discord, activity);
            discord.RunCallbacks();
        }

        protected override void OnStop()
        {
            WriteToLog("Service stopped at " + DateTime.Now.ToString());
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
            if (activity.Timestamps.End == 1)
            {
                activity.Timestamps.End = GetEndTime();
            }
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
