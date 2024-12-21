using Chronokeep.UI.UIObjects;
using Hardware.Info;
using System.Text;
using System;
using System.Windows;
using System.Windows.Threading;

namespace Chronokeep.Helpers
{
    class HardwareChecker
    {
        private IDBInterface database;

        public HardwareChecker(IDBInterface database)
        {
            this.database = database;
        }

        public void Run()
        {
            try
            {
                Log.D("Helpers.HardwareChecker", "Fetching hardware information.");
                IHardwareInfo hardwareInfo = new HardwareInfo();
                hardwareInfo.RefreshOperatingSystem();
                hardwareInfo.RefreshCPUList(false, 10);
                hardwareInfo.RefreshMemoryList();
                hardwareInfo.RefreshVideoControllerList();
                StringBuilder hardwareIDBuilder = new();
                hardwareIDBuilder.AppendFormat("{0}-", hardwareInfo.OperatingSystem.Name);
                uint cpuCount = 0;
                uint coreCount = 0;
                uint processorCount = 0;
                StringBuilder cpuComposite = new();
                foreach (var cpu in hardwareInfo.CpuList)
                {
                    cpuCount++;
                    coreCount += cpu.NumberOfCores;
                    processorCount += cpu.NumberOfLogicalProcessors;
                    cpuComposite.AppendFormat("{0}+", cpu.Name);
                }
                cpuComposite.Remove(cpuComposite.Length - 1, 1);
                hardwareIDBuilder.AppendFormat("{0}-{1}C-{2}P-", cpuComposite, coreCount, processorCount);
                uint memoryCount = 0;
                ulong totalCapacity = 0;
                foreach (var memory in hardwareInfo.MemoryList)
                {
                    memoryCount++;
                    totalCapacity += memory.Capacity;
                }
                int reductionNum = 0;
                while (totalCapacity > 1024)
                {
                    reductionNum++;
                    totalCapacity = totalCapacity / 1024;
                }
                string byteType;
                switch (reductionNum)
                {
                    case 0:
                        byteType = "B";
                        break;
                    case 1:
                        byteType = "KB";
                        break;
                    case 2:
                        byteType = "MB";
                        break;
                    case 3:
                        byteType = "GB";
                        break;
                    case 4:
                        byteType = "TB";
                        break;
                    default:
                        byteType = "??";
                        break;
                }
                hardwareIDBuilder.AppendFormat("{0}@{1}{2}-", memoryCount, totalCapacity, byteType);
                int videoCount = 0;
                StringBuilder videoComposite = new();
                foreach (var video in hardwareInfo.VideoControllerList)
                {
                    videoCount++;
                    videoComposite.AppendFormat("{0}+", video.Name);
                }
                videoComposite.Remove(videoComposite.Length - 1, 1);
                hardwareIDBuilder.Append(videoComposite);
                hardwareIDBuilder.Replace(' ', '_');
                string hwID = hardwareIDBuilder.ToString();
                Log.D("Helpers.HardwareChecker", $"Unique Identifier: '{hwID}'");
                AppSetting hardwareSetting = database.GetAppSetting(Constants.Settings.HARDWARE_IDENTIFIER);
                if (hardwareSetting != null)
                {
                    if (!hardwareSetting.Value.Equals(hwID, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.D("Helpers.HardwareChecker", "Hardware identifier appears to have changed.");
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate ()
                        {
                            DialogBox.Show(
                                "We've detected that our database file may have been transferred from a different computer. Would you like to change the program's unique identifier to ensure there are no conflicts between devices?",
                                "Yes",
                                "No",
                                () =>
                                {
                                    string randomMod = Constants.Settings.AlphaNumRegex().Replace(Guid.NewGuid().ToString("N"), "").ToUpper()[0..3];
                                    database.SetAppSetting(Constants.Settings.PROGRAM_UNIQUE_MODIFIER, randomMod);
                                }
                                );
                        }));
                    }
                }
                database.SetAppSetting(Constants.Settings.HARDWARE_IDENTIFIER, hwID);
            }
            catch (Exception ex)
            {
                Log.E("Helpers.HardwareChecker", $"Error getting hardware information. {ex.Message}");
            }
        }
    }
}
