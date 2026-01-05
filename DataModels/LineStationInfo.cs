using System.Collections.Generic;

namespace FirstPersonCameraContinued.DataModels
{
    public class LineStationInfo
    {
        public List<StationData> stations { get; set; } = new List<StationData>();
        public int currentStopIndex { get; set; } = 0;
        public string lineColor { get; set; } = "rgb(255, 255, 255)";
    }

    public class StationData
    {
        public string name { get; set; } = "";
    }
}
