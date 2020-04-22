using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Lucky.Home.Devices.Garden
{
    class MailScheduler
    {
        private readonly GardenDevice _device;
        private readonly string _mailTitle;
        private readonly string _mailHeader;
        private List<MailData> _mailQueue = new List<MailData>();
        private Timer _wakeUpTimer;

        private class MailData
        {
            public string Name;
            public ZoneTimeWithQuantity[] ZoneData;

            public string ToString(MailScheduler owner)
            {
                if (ZoneData != null)
                {
                    // Format with ZoneTimeWithQuantity list
                    return string.Format("{0} \r\n{1}",
                        Name,
                        string.Join(Environment.NewLine,
                            ZoneData.Select(t =>
                            {
                                return string.Format(Resources.gardenMailBody, owner.GetZoneNames(t.Zones), t.Minutes, t.QuantityL);
                            })
                        ));
                }
                else
                {
                    return string.Format(Resources.gardenMailSuspendedCycle, Name);
                }
            }
        }

        public MailScheduler(GardenDevice device, string mailTitle, string mailHeader)
        {
            _device = device;
            _mailTitle = mailTitle;
            _mailHeader = mailHeader;
        }

        public void ScheduleMail(DateTime now, string name, ZoneTimeWithQuantity[] results = null)
        {
            lock (_mailQueue)
            {
                _mailQueue.Add(new MailData
                {
                    Name = name,
                    ZoneData = results
                });
            }
            CheckSend(now);
        }

        private string GetZoneNames(int[] index)
        {
            return string.Join(", ", index.Select(i => _device.GetZoneName(i)));
        }

        private void CheckSend(DateTime now)
        {
            // If more programs will follow, don't send the mail now
            var nextCycle = _device.GetNextCycle(now);
            if ((nextCycle == null || nextCycle.Item2 > (now + TimeSpan.FromMinutes(65))) && !_device.InUse)
            {
                SendNow();
            }
            else
            {
                // Schedule a wake-up timer, in case the next program will not be called for whatever reason
                _wakeUpTimer?.Dispose();
                _wakeUpTimer = new Timer(o =>
                {
                    SendNow();
                }, null, TimeSpan.FromMinutes(66), Timeout.InfiniteTimeSpan);
            }
        }

        private void SendNow()
        {
            _wakeUpTimer?.Dispose();

            // Schedule mail
            string body = _mailHeader + Environment.NewLine;
            lock (_mailQueue)
            {
                body += string.Join(
                    Environment.NewLine,
                    _mailQueue.Select(data => data.ToString(this))
                );
                _mailQueue.Clear();
            }
            Manager.GetService<INotificationService>().SendMail(_mailTitle, body, false);
        }
    }
}
