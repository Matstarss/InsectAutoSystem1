using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsectAutoSystem1
{
    public static class DeviceState
    {
        public enum FeedState
        {
            None,
            NewBox,
            Feeding,
            Full,
            End
        }

        private static FeedState feedState = FeedState.None;
        private static bool sensorState = false;
        public static double targetFeedWeight = 6.0;

        public static void setFeedState(FeedState state)
        {
            feedState = state;
        }

        public static FeedState getFeedState()
        {
            return feedState;
        }


    }
}
