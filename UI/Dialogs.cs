using System;
using Parkitect.UI;
using UnityEngine;

namespace ModUIToolkit.UI {
    // TODO: Verify which dialogs can open from the main menu; ModLoadoutManager shows popups there.
    /// <summary>Opens Parkitect's name prompt and confirmation window, or shows its red error notice.</summary>
    public static class Dialogs {
        // TODO: Check whether the name prompt can open from the main menu.
        /// <summary>Opens Parkitect's rename prompt with a suggested name.</summary>
        /// <remarks>
        /// Calls onConfirmed with the entered name when the player confirms with Enter or the button. An exception thrown by
        /// onConfirmed is logged through UILog and the prompt still closes.
        /// </remarks>
        /// <returns>
        /// False if the window controller is unavailable, the window could not be created, or opening the prompt fails.
        /// </returns>
        public static bool PromptForName(string title, string suggested, Action<string> onConfirmed) {
            UILog.Debug("Dialogs.PromptForName: " + title);
            try {
                var controller = UIWindowsController.Instance;
                if (controller == null) {
                    UILog.Info("Dialogs.PromptForName: no park loaded");
                    return false;
                }
                var renameWindow = controller.spawnWindowFromPrefab<RenameWindow>(ThemedPrefabs.Require(m => m.renameWindowGO));
                if (renameWindow == null) return false;
                if (renameWindow.windowFrame != null && !string.IsNullOrEmpty(title)) renameWindow.windowFrame.setTitle(title);
                renameWindow.setName(suggested);
                if (onConfirmed != null) renameWindow.OnConfirmed += name => RunCallback("Dialogs.PromptForName", () => onConfirmed(name));
                return true;
            } catch (Exception e) {
                UILog.Error("Dialogs.PromptForName failed: " + e);
                return false;
            }
        }

        /// <summary>Opens Parkitect's confirmation window with the given text and title.</summary>
        /// <remarks>An exception thrown by onConfirmed or onCanceled is logged through UILog and the window still closes.</remarks>
        /// <returns>
        /// The window, or null if the window controller is unavailable or opening the confirmation fails.
        /// </returns>
        public static ConfirmationWindow Confirm(string text, string title, Action onConfirmed, Action onCanceled = null) {
            UILog.Debug("Dialogs.Confirm: " + text);
            try {
                if (UIWindowsController.Instance == null) return null;
                var confirmed = new ConfirmationWindow.OnConfirmedHandler(() => RunCallback("Dialogs.Confirm", onConfirmed));
                var canceled = onCanceled != null ? new ConfirmationWindow.OnCanceledHandler(() => RunCallback("Dialogs.Confirm cancel", onCanceled)) : null;
                return ConfirmationWindow.launch(text, title, confirmed, canceled);
            } catch (Exception e) {
                UILog.Error("Dialogs.Confirm failed: " + e);
                return null;
            }
        }

        /// <summary>Shows Parkitect's red error notice near the cursor.</summary>
        public static void ShowError(string message) {
            UILog.Debug("Dialogs.ShowError: " + message);
            try {
                ErrorMessageController.showErrorMessage(message);
            } catch (Exception e) {
                UILog.Error("Dialogs.ShowError failed (" + message + "): " + e);
            }
        }

        // The game's dialogs invoke their callback before closing themselves, so an exception from a mod callback would leave the dialog open and let the player confirm it again. Contain it here so the dialog still closes.
        private static void RunCallback(string context, Action callback) {
            if (callback == null) return;
            try {
                callback();
            } catch (Exception e) {
                UILog.Error(context + " callback threw: " + e);
            }
        }

        // TODO: Add the game's error popup for exceptions, and distinguish it from the tooltip shown by ShowError.
    }
}
