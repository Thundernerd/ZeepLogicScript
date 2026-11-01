using System;
using System.Collections.Generic;
using Imui.IO.Events;
using Imui.IO.UGUI;
using UnityEngine;

namespace TNRD.Zeepkist.LogicScripting.InGameTests;

/// <summary>
/// Queues keys on Imui's backend the way its <c>OnGUI</c> queues the ones Unity reports, so the next frame's fields
/// handle them as typed. Unity's own <c>Input</c> never sees them.
/// </summary>
internal static class ImuiKeys
{
    private static ImuiUnityGUIBackend _backend;

    /// <summary>
    /// Returns false when there's no backend to queue them on.
    /// </summary>
    public static bool Queue(IEnumerable<KeyPress> keys)
    {
        if (_backend == null)
            _backend = UnityEngine.Object.FindObjectOfType<ImuiUnityGUIBackend>();

        if (_backend == null)
            return false;

        foreach (var key in keys)
        {
            var modifiers = (EventModifiers)key.Modifiers;
            var code = (KeyCode)Enum.Parse(typeof(KeyCode), key.Key);
            if (code != KeyCode.None)
                _backend.nextKeyboardEvents.PushFront(new ImKeyboardEvent(ImKeyboardEventType.Down, code, modifiers, '\0'));

            if (key.Character != '\0')
                _backend.nextKeyboardEvents.PushFront(new ImKeyboardEvent(ImKeyboardEventType.Down, KeyCode.None, modifiers, key.Character));
        }

        return true;
    }
}
