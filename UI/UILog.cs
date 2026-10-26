using System;

namespace ModUIToolkit.UI {
    /// <summary>Writes messages to Unity's log, with optional debug output and a mod-name prefix.</summary>
    /// <remarks>
    /// Works without setup. Set Prefix to your mod's name and MinLevel to Debug when troubleshooting.
    /// If your mod already has a logger, MessageHandler can send messages there instead.
    /// </remarks>
    public static class UILog {
        public enum Level {
            Debug = 0,
            Info = 1,
            Error = 2,
        }

        /// <summary>Messages below this level are discarded.</summary>
        public static Level MinLevel = Level.Info;

        /// <summary>Prefix added to messages sent directly to Unity's log.</summary>
        public static string Prefix = "ModUIToolkit";

        /// <summary>Optional callback that sends messages to your own logger instead of Unity's log.</summary>
        /// <remarks>
        /// Receives messages that pass MinLevel, without Prefix. Leave null to use the built-in Unity output.
        /// </remarks>
        public static Action<Level, string> MessageHandler;

        public static void Debug(string message) {
            Write(Level.Debug, message);
        }

        public static void Info(string message) {
            Write(Level.Info, message);
        }

        public static void Error(string message) {
            Write(Level.Error, message);
        }

        private static void Write(Level level, string message) {
            if (level < MinLevel) return;
            if (MessageHandler != null) {
                MessageHandler(level, message);
                return;
            }
            switch (level) {
                case Level.Debug: {
                        UnityEngine.Debug.Log(Prefix + ": [debug] " + message);
                        break;
                    }
                case Level.Info: {
                        UnityEngine.Debug.Log(Prefix + ": [info] " + message);
                        break;
                    }
                case Level.Error: {
                        UnityEngine.Debug.LogError(Prefix + ": [error] " + message);
                        break;
                    }
            }
        }
    }
}
