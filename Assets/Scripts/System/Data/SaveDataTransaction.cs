using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buffers PlayerPrefs writes while a cutscene is running.
/// A naturally completed cutscene commits the buffer. An interrupted
/// cutscene discards it, leaving the previous disk save untouched.
/// </summary>
public static class SaveDataTransaction
{
    private enum ValueType
    {
        Integer,
        String,
        Deleted
    }

    private struct PendingValue
    {
        public ValueType type;
        public int intValue;
        public string stringValue;
    }

    private static readonly Dictionary<string, PendingValue> pending =
        new();
    private static bool writesBlocked;

    public static bool IsActive { get; private set; }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        pending.Clear();
        IsActive = false;
        writesBlocked = false;
    }

    public static void Begin()
    {
        if (IsActive)
            return;

        pending.Clear();
        writesBlocked = false;
        IsActive = true;
    }

    public static void Commit()
    {
        if (!IsActive)
            return;

        foreach (KeyValuePair<string, PendingValue> pair in pending)
        {
            switch (pair.Value.type)
            {
                case ValueType.Integer:
                    PlayerPrefs.SetInt(
                        pair.Key,
                        pair.Value.intValue
                    );
                    break;

                case ValueType.String:
                    PlayerPrefs.SetString(
                        pair.Key,
                        pair.Value.stringValue
                    );
                    break;

                case ValueType.Deleted:
                    PlayerPrefs.DeleteKey(pair.Key);
                    break;
            }
        }

        pending.Clear();
        IsActive = false;
        writesBlocked = false;
        PlayerPrefs.Save();
    }

    public static void Rollback()
    {
        pending.Clear();
        IsActive = false;
        writesBlocked = true;
    }

    public static void SetInt(string key, int value)
    {
        if (writesBlocked)
            return;

        if (!IsActive)
        {
            PlayerPrefs.SetInt(key, value);
            return;
        }

        pending[key] = new PendingValue
        {
            type = ValueType.Integer,
            intValue = value
        };
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (pending.TryGetValue(key, out PendingValue value))
        {
            if (value.type == ValueType.Deleted)
                return defaultValue;

            if (value.type == ValueType.Integer)
                return value.intValue;
        }

        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public static void SetString(string key, string value)
    {
        if (writesBlocked)
            return;

        if (!IsActive)
        {
            PlayerPrefs.SetString(key, value);
            return;
        }

        pending[key] = new PendingValue
        {
            type = ValueType.String,
            stringValue = value
        };
    }

    public static string GetString(
        string key,
        string defaultValue = "")
    {
        if (pending.TryGetValue(key, out PendingValue value))
        {
            if (value.type == ValueType.Deleted)
                return defaultValue;

            if (value.type == ValueType.String)
                return value.stringValue;
        }

        return PlayerPrefs.GetString(key, defaultValue);
    }

    public static bool HasKey(string key)
    {
        if (pending.TryGetValue(key, out PendingValue value))
            return value.type != ValueType.Deleted;

        return PlayerPrefs.HasKey(key);
    }

    public static void DeleteKey(string key)
    {
        if (writesBlocked)
            return;

        if (!IsActive)
        {
            PlayerPrefs.DeleteKey(key);
            return;
        }

        pending[key] = new PendingValue
        {
            type = ValueType.Deleted
        };
    }

    public static void Save()
    {
        if (!IsActive && !writesBlocked)
            PlayerPrefs.Save();
    }
}
