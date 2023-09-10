import { Strings as itStrings } from './resources.it-IT';

export const Strings = {
    "Admin_Halt": "Halt Net Server",
    "Admin_Start": "Start Net Server",
    "Admin_Restart_Garden": "Restart Garden Server",
    "Admin_Restart_Solar": "Restart Solar Server",
    "Admin_DownloadGardenCsv": "Download garden.csv",
    "Admin_DownloadGardenConfig": "Download garden configuration",
    "Admin_UploadGardenConfig": "Upload garden configuration",

    "Error": (err: string) => `Error: ${err}`,

    "Device_StatusLoading": "Loading...",
    "Device_StatusOnline": "Online",
    "Device_StatusPartiallyOnline": "Partially Online",
    "Device_StatusOffline": "OFFLINE",

    "Garden_QuickCycle": "Quick cycle",
    "Garden_Minutes": "Minutes: ",
    "Garden_AddImmediate": "Add manual cycle",
    "Garden_ClearImmediate": "Clear",
    "Garden_StartImmediate": "Go!",
    "Garden_StartedImmediate": "Started",
    "Garden_ImmediateError": (err: string) => `Error starting: ${err}`,
    "Garden_Stop": "STOP",
    "Garden_Stopped": "Stopped!",
    "Garden_StopError": (err: string) => `Error stopping: ${err}`,
    "Garden_NextCycles": "Next programmmed cycles:",
    "Garden_ScheduledProgram": (args: any) => `${args.name} program scheduled for ${args.scheduledTime}`,
    "Garden_RunningProgram": (args: any) => `${args.name} program running`,
    "Garden_QueuedProgram": (args: any) => `${args.name} program in queue`,
    "Garden_FlowInfo": "Flow:",
    "Garden_MissingConf": "Missing configuration",
    "Garden_ErrorConf": (err: string) => `Cannot fetch configuration: ${err}`,
    "Garden_ErrorSetConf": (err: string) => `Invalid configuration data: ${err}`,
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
    "Solar_Updated": (args: any) => `Up-to-date at ${args.currentTs}.`,
    "Solar_Peak1": "Peak of ",
    "Solar_Peak2": (args: any) => ` at ${args.ts}`,
    "Solar_CurrentUsage": "Current usage of ",
    "Solar_Off": "OFF",
    "Solar_On": (args: any) => `Power: ${args.power}W`,
    "Solar_FaultNoGrid": "No grid power"
};

/**
 * TODO: use a proper i18n service
 */
const res = { ...Strings, ...itStrings };
const format = (str: string, args: any) => (typeof (res as any)[str] === "function") ? (res as any)[str](args): (res as any)[str];

export { res, format };
