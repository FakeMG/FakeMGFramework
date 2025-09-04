using TMPro;
using UnityEngine;

namespace FakeMG.Framework.Timer
{
    public class TimerTextUIUpdater : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timerText;
        private readonly char[] _timeFormatBuffer = new char[5]; // "MM:SS"

        public void UpdateUI(int currentTimeInSeconds)
        {
            if (currentTimeInSeconds <= 0)
            {
                timerText.text = "00:00";
                return;
            }

            int minutes = currentTimeInSeconds / 60;
            int seconds = currentTimeInSeconds % 60;

            // Format minutes
            _timeFormatBuffer[0] = (char)('0' + minutes / 10);
            _timeFormatBuffer[1] = (char)('0' + minutes % 10);
            _timeFormatBuffer[2] = ':';
            // Format seconds
            _timeFormatBuffer[3] = (char)('0' + seconds / 10);
            _timeFormatBuffer[4] = (char)('0' + seconds % 10);

            timerText.SetText(_timeFormatBuffer, 0, 5);
        }
    }
}