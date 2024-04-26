using Lucky.Db;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace Lucky.Home.Solar
{
    public enum OperatingState
    {
        [CsvValue("WAIT")]
        Waiting = 0,
        [CsvValue("CHK")]
        Checking = 1,
        [CsvValue("")]
        Normal = 2,
        [CsvValue("FAULT")]
        Fault = 3,
        [CsvValue("PERM_FAULT")]
        PermanentFault = 4,

        FirstUnknownValue = 5,

        [CsvValue("UNKNOWN")]
        Unknown = -1
    }

    /// <summary>
    /// Known inverter states. Unknown state will be logged with original flags
    /// </summary>
    [DataContract]
    public class InverterState
    {
        private static Dictionary<OperatingState, string> s_valueToCsvValue = new Dictionary<OperatingState, string>();
        private static Dictionary<string, OperatingState> s_csvValueToValue = new Dictionary<string, OperatingState>();
        static InverterState()
        {
            var memberInfos = typeof(OperatingState).GetMembers();
            foreach (var info in memberInfos)
            {
                var valueAttribute = info.GetCustomAttribute<CsvValueAttribute>();
                if (valueAttribute != null)
                {
                    s_valueToCsvValue[Enum.Parse<OperatingState>(info.Name)] = valueAttribute.Value;
                    s_csvValueToValue[valueAttribute.Value] = Enum.Parse<OperatingState>(info.Name);
                }
            }
        }

        public InverterState()
        {

        }

        public InverterState(OperatingState operatingState)
        {
            OperatingState = operatingState;
        }

        public InverterState(OperatingState operatingState, string faultBits)
        {
            OperatingState = operatingState;
            FaultBits = faultBits;
        }

        [DataMember]
        public OperatingState OperatingState { get; set; }
        [DataMember]
        public string FaultBits { get; set; }

        internal string ToCsv()
        {
            string value;
            if (!s_valueToCsvValue.TryGetValue(OperatingState, out value))
            {
                value = "UNKNOWN";
            }
            if (OperatingState == OperatingState.Normal)
            {
                return value;
            }
            else
            {
                return value + ":" + FaultBits;
            }
        }

        internal static InverterState FromCsv(string value)
        {
            var parts = value.Split(":");
            OperatingState enumValue;
            if (!s_csvValueToValue.TryGetValue(parts[0], out enumValue))
            {
                enumValue = OperatingState.Unknown;
            }
            if (parts.Length == 2)
            {
                return new InverterState(enumValue, parts[1]);
            }
            else
            {
                return new InverterState(enumValue);
            }
        }

        internal string ToUserInterface(Device.Sofar.NightState inverterNightState) 
        {
            if (inverterNightState == Device.Sofar.NightState.Night)
            {
                return "Off";
            }
            else
            {
                return OperatingState.ToString();
            }
        }

        internal bool IsFault
        {
            get
            {
                return OperatingState != OperatingState.Normal && OperatingState != OperatingState.Waiting && OperatingState != OperatingState.Checking;
            }
        }

        internal string IsFaultToReport()
        {
            if (IsFault)
            {
                return ToCsv();
            }
            else
            {
                return null;
            }
        }
    }
}
