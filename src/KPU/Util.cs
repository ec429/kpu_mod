using System;

namespace KPU
{
    public static class Logging
    {
        public static void Log(string message)
        {
            UnityEngine.Debug.Log("KPU: " + message);
            #if DEBUG
            try
            {
                var msg = new ScreenMessage("KPU: debug: " + message, 4f, ScreenMessageStyle.UPPER_LEFT);
                ScreenMessages.PostScreenMessage(msg);
            }
            catch (Exception)
            {
            }
            #endif
        }
    }
}

