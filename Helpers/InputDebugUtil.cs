using System;
using System.Linq;
using System.Reflection;
using UnityEngine.InputSystem;
using Game.Input;

static class InputDebugUtil
{
    public static void DumpActionNames(ProxyActionMap map, Action<string> log)
    {
        foreach (var kv in map.actions)
            log($"[Shortcuts] Action: {kv.Key}");
    }

    public static void DumpActionBindings(ProxyActionMap map, Action<string> log)
    {
        foreach (var kv in map.actions)
        {
            var ia = GetInputAction(kv.Value);
            if (ia == null)
            {
                log($"[Shortcuts] {kv.Key}: (no InputAction via reflection)");
                continue;
            }

            for (int i = 0; i < ia.bindings.Count; i++)
            {
                var b = ia.bindings[i];
                // 'path' is the default; 'overridePath' may be used if the player rebinds
                var path = string.IsNullOrEmpty(b.overridePath) ? b.path : b.overridePath;
                log($"[Shortcuts] {ia.name} => {path}  (groups: {b.groups})");
            }
        }
    }

    public static ProxyAction[] FindActionsBoundToKeys(ProxyActionMap map, params string[] keyPaths)
    {
        var wanted = keyPaths.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return map.actions.Values
            .Where(pa =>
            {
                var ia = GetInputAction(pa);
                if (ia == null) return false;
                return ia.bindings.Any(b =>
                {
                    var path = string.IsNullOrEmpty(b.overridePath) ? b.path : b.overridePath;
                    // match exact control path like "<Keyboard>/1"
                    return wanted.Contains(path);
                });
            })
            .ToArray();
    }

    // Robust reflection to pull the wrapped Unity InputAction from ProxyAction
    static InputAction GetInputAction(ProxyAction proxy)
    {
        var t = proxy.GetType();

        // Try a few likely private fields/properties
        var fld = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(f => typeof(InputAction).IsAssignableFrom(f.FieldType));
        if (fld != null) return fld.GetValue(proxy) as InputAction;

        var prop = t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                    .FirstOrDefault(p => typeof(InputAction).IsAssignableFrom(p.PropertyType) && p.CanRead);
        if (prop != null) return prop.GetValue(proxy) as InputAction;

        return null;
    }
}
