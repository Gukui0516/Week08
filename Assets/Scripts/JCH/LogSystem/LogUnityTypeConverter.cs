// LogUnityTypeConverter.cs
using UnityEngine;

/// <summary>
/// Unity 타입 전용 로그 문자열 변환기
/// </summary>
public static class LogUnityTypeConverter
{
    #region Public Methods - Type Conversion
    /// <summary>
    /// Unity 타입을 로그 문자열로 변환
    /// </summary>
    /// <param name="value">변환할 Unity 타입 값</param>
    /// <returns>변환된 문자열</returns>
    public static string Convert(object value)
    {
        return value switch
        {
            Vector2 v => ConvertVector2(v),
            Vector3 v => ConvertVector3(v),
            Vector4 v => ConvertVector4(v),
            Quaternion q => ConvertQuaternion(q),
            Color c => ConvertColor(c),
            Color32 c => ConvertColor32(c),
            Rect r => ConvertRect(r),
            Bounds b => ConvertBounds(b),
            _ => value?.ToString() ?? "null"
        };
    }
    #endregion

    #region Private Methods - Vector Types
    private static string ConvertVector2(Vector2 v)
    {
        return $"({v.x:F2},{v.y:F2})";
    }

    private static string ConvertVector3(Vector3 v)
    {
        return $"({v.x:F2},{v.y:F2},{v.z:F2})";
    }

    private static string ConvertVector4(Vector4 v)
    {
        return $"({v.x:F2},{v.y:F2},{v.z:F2},{v.w:F2})";
    }
    #endregion

    #region Private Methods - Rotation Types
    private static string ConvertQuaternion(Quaternion q)
    {
        return $"({q.x:F2},{q.y:F2},{q.z:F2},{q.w:F2})";
    }
    #endregion

    #region Private Methods - Color Types
    private static string ConvertColor(Color c)
    {
        return $"RGBA({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})";
    }

    private static string ConvertColor32(Color32 c)
    {
        return $"RGBA({c.r},{c.g},{c.b},{c.a})";
    }
    #endregion

    #region Private Methods - Geometric Types
    private static string ConvertRect(Rect r)
    {
        return $"Rect(x:{r.x:F1},y:{r.y:F1},w:{r.width:F1},h:{r.height:F1})";
    }

    private static string ConvertBounds(Bounds b)
    {
        return $"Bounds(center:{ConvertVector3(b.center)},size:{ConvertVector3(b.size)})";
    }
    #endregion
}