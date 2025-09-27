using UnityEngine;

/// <summary>
/// Overrides UnityEngine.Debug to mute debug messages completely on a platform-specific basis.
/// This can be placed inside the 'Plugins' folder.
/// 
/// Important:
///     Preprocessor directives other than 'UNITY_EDITOR' may not work correctly.
/// 
/// Note:
///     The [Conditional] attribute indicates to compilers that a method call or attribute 
///     should be ignored unless a specified conditional compilation symbol is defined.
/// 
/// See Also: 
///     http://msdn.microsoft.com/en-us/library/system.diagnostics.conditionalattribute.aspx
/// 
/// 2012.11. @kimsama
/// </summary>
public static class Debug
{
    public static bool isDebugBuild
    {
        get { return UnityEngine.Debug.isDebugBuild; }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message)
    {
        UnityEngine.Debug.Log(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Log(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.Log(message, context);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message)
    {
        UnityEngine.Debug.LogError(message);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogError(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.LogError(message, context);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message)
    {
        UnityEngine.Debug.LogWarning(message.ToString());
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogWarning(object message, UnityEngine.Object context)
    {
        UnityEngine.Debug.LogWarning(message.ToString(), context);
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogException(System.Exception exception)
    {
        UnityEngine.Debug.LogException(exception);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawLine(Vector3 start, Vector3 end, Color color = default(Color), float duration = 0.0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawLine(start, end, color, duration, depthTest);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawRay(Vector3 start, Vector3 dir, Color color = default(Color), float duration = 0.0f, bool depthTest = true)
    {
        UnityEngine.Debug.DrawRay(start, dir, color, duration, depthTest);
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void DrawSphere(Vector3 position, float radius, Color color, int segments = 16, float duration = 0.1f, bool depthTest = true)
    {
        // 원형으로 구를 표현하기 위한 각도 계산
        float step = 360f / segments;

        // XZ 평면에서의 원
        for (float angle = 0; angle < 360f; angle += step)
        {
            float rad1 = Mathf.Deg2Rad * angle;
            float rad2 = Mathf.Deg2Rad * (angle + step);

            Vector3 p1 = position + new Vector3(Mathf.Cos(rad1) * radius, 0, Mathf.Sin(rad1) * radius);
            Vector3 p2 = position + new Vector3(Mathf.Cos(rad2) * radius, 0, Mathf.Sin(rad2) * radius);

            Debug.DrawLine(p1, p2, color, duration, depthTest);
        }

        // XY 평면에서의 원
        for (float angle = 0; angle < 360f; angle += step)
        {
            float rad1 = Mathf.Deg2Rad * angle;
            float rad2 = Mathf.Deg2Rad * (angle + step);

            Vector3 p1 = position + new Vector3(Mathf.Cos(rad1) * radius, Mathf.Sin(rad1) * radius, 0);
            Vector3 p2 = position + new Vector3(Mathf.Cos(rad2) * radius, Mathf.Sin(rad2) * radius, 0);

            Debug.DrawLine(p1, p2, color, duration, depthTest);
        }

        // YZ 평면에서의 원
        for (float angle = 0; angle < 360f; angle += step)
        {
            float rad1 = Mathf.Deg2Rad * angle;
            float rad2 = Mathf.Deg2Rad * (angle + step);

            Vector3 p1 = position + new Vector3(0, Mathf.Cos(rad1) * radius, Mathf.Sin(rad1) * radius);
            Vector3 p2 = position + new Vector3(0, Mathf.Cos(rad2) * radius, Mathf.Sin(rad2) * radius);

            Debug.DrawLine(p1, p2, color, duration, depthTest);
        }
    }
    
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void Assert(bool condition)
    {
        if (!condition) throw new System.Exception();
    }
}
