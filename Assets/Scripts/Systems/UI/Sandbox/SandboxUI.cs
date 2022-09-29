using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SandboxUI : MonoSingleton<SandboxUI>
{
    [SerializeField]
    private Text cursorPositionText;

    public string CursorPositionText { set => cursorPositionText.text = value; }
}
