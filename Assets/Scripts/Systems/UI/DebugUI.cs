using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Apes.UI
{
    public class DebugUI : MonoSingleton<DebugUI>
    {
        [SerializeField]
        private Text debugMessageText;

        [SerializeField]
        private Text fpsText;

        [SerializeField]
        private Text tickText;

        public string MessageText { set => debugMessageText.text = value; }
        public string FpsText { set => fpsText.text = value; }
        public string TickText { set => tickText.text = value; }
    }
}