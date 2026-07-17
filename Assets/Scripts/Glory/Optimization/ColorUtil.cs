using UnityEngine;

public static class ColorUtil
{
    public static Color GetColorHtml(string _hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString("#" + _hex, out color) == false)
        {
            Logger.Error($"[ColorUtil] GetColorHtml Failed! parse error - {_hex}");
            return Color.white;
        }

        return color;
    }
}
