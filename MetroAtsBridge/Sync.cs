using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MetroAtsBridge
{
    public static partial class Sync {
        private const CallingConvention CalCnv = CallingConvention.StdCall;
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void Load();
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void Dispose();
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void SetVehicleSpec(AtsVehicleSpec s);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void Initialize(int s);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern unsafe AtsHandles Elapse(AtsVehicleState s, IntPtr Pa, IntPtr So);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void SetPower(int p);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void SetBrake(int b);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void SetReverser(int r);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void KeyDown(int k);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void KeyUp(int k);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void HornBlow(int k);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void DoorOpen();
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void DoorClose();
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void SetSignal(int s);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void SetBeaconData(AtsBeaconData b);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void setATOTASCStatus(int mode);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void ATO_setATCLimit(double distance, int signalindex);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void ATO_setSignalLimit(double distance, double speed);
        [DllImport("bve-autopilot-lib.dll", CallingConvention = CalCnv)]
        public static extern void ATOTASC_setSignalMaxDecel(double dec);
    }
}
