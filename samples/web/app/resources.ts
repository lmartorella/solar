import { Strings as itStrings } from './resources.it-IT';

export const Strings = {
    "Menu_Logs": "Logs",
    "Admin_Halt": "Halt Server",
    "Admin_Start": "Start Server",
    "Admin_Restart": "Restart Server",
    "Admin_DownloadGardenCsv": "Download garden.csv",
    "Admin_DownloadGardenConfig": "Download garden configuration",
    "Admin_UploadGardenConfig": "Upload garden configuration",

    "Error": err => `Error: ${err}`,

    "Device_StatusLoading": "Loading...",
    "Device_StatusOnline": "Online",
    "Device_StatusOffline": "OFFLINE",

    "Garden_QuickCycle": "Quick cycle",
    "Garden_Minutes": "Minutes: ",
    "Garden_AddImmediate": "Add manual cycle",
    "Garden_ClearImmediate": "Clear",
    "Garden_StartImmediate": "Go!",
    "Garden_StartedImmediate": "Started",
    "Garden_ImmediateError": err => `Error starting: ${err}`,
    "Garden_Stop": "STOP",
    "Garden_Stopped": "Stopped!",
    "Garden_StopError": err => `Error stopping: ${err}`,
    "Garden_NextCycles": "Next programmmed cycles:",
    "Garden_ScheduledProgram": (args) => `${args.name} program scheduled for ${args.scheduledTime}`,
    "Garden_RunningProgram": (args) => `${args.name} program running`,
    "Garden_QueuedProgram": (args) => `${args.name} program in queue`,
    "Garden_FlowInfo": "Flow:",
    "Garden_MissingConf": "Missing configuration",
    "Garden_ErrorConf": err => `Cannot fetch configuration: ${err}`,
    "Garden_ErrorSetConf": err => `Invalid configuration data: ${err}`,
    "Garden_Suspended": " (suspended)",
    "Garden_SuspendAll": "Suspend for Rain",
    "Garden_ResumeAll": "Resume from Rain",
    "Garden_EditProgram": "Edit program",
    "Garden_SuspendedCheckbox": "Suspended:",
    "Garden_DisabledCheckbox": "Disabled:",
    "Garden_StartAt": "Start at:",
    "Garden_Duration": "Duration (min):",
    "Garden_SaveProgram": "Save Program",
    "Garden_ClearProgram": "Clear",

    "Solar_ChartToday": "Chart today",
    "Solar_Chart4days": "Chart last 4 days",
    "Solar_EnergyToday": "Today's energy:",
    "Solar_EnergyTotal": "Total energy:",
    "Solar_Updated": args => `Update at ${args.currentTs}.`,
    "Solar_Off": "OFF",
    "Solar_On": args => `Power: ${args.power}W`,
    "Solar_FaultNoGrid": "No grid power",
    "Solar_FaultLowFreq": "Grid frequency too low",
    "Solar_FaultHighFreq": "Grid frequency too high"
};

const res = { ...Strings, ...itStrings };
const format = (str, args) => (typeof res[str] === "function") ? res[str](args): res[str];

export { res, format };
