using System;

namespace KPU
{
    public static class Logging
    {
        public static void Log(string message)
        {
            UnityEngine.Debug.Log("KPU: " + message);
            #if DEBUG
            var msg = new ScreenMessage("KPU: debug: " + message, 4f, ScreenMessageStyle.UPPER_LEFT);
            ScreenMessages.PostScreenMessage(msg);
            #endif
        }
    }
}

