using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Cursor", menuName = "Cursor")]
public class CursorType : ScriptableObject
{
    public Texture2D cursorTexture;
    public Vector2 cursorHotspot;
}
