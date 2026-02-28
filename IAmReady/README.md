# I Am Ready

**I Am Ready** is a Dalamud plugin for Final Fantasy XIV that automatically accepts or declines Party and Alliance Ready Checks based on a customizable counter. 

Whether you are stepping away for a moment or just tired of clicking, this plugin handles ready checks for you while mimicking human reaction times.

## ✨ Features

* **Auto-Answer Ready Checks:** Automatically clicks "Yes" or "No" when a ready check appears.
* **Configurable "Yes" Counter:** Set a specific number of times you want to automatically answer "Yes". Once that limit is reached, the plugin will automatically start answering "No" to prevent pulling when you are actually away.
* **Natural Delay:** Enable a randomized delay (0.7 to 2.5 seconds) before the plugin answers, making it look like a natural human reaction.
* **Smart Chat Triggers (Regex):** Add custom text phrases to the plugin. If someone says one of these phrases in Party or Alliance chat (e.g., "reset", "pulling"), your counter will automatically reset back to zero.
* **Bilingual Support:** The user interface is available in both English and German.

## 🚀 Usage

1. Type `/iar` in the in-game chat to open the configuration window.
2. Click the **Inactive/Active** button at the top to turn the auto-responder on or off.
3. Adjust the **Yes Count** slider. For example, if set to `5`, the plugin will answer "Yes" to the next 5 ready checks, and "No" to any checks after that.
4. Click the **Start** button at any time to manually reset your current counter back to zero.

### Chat Triggers (Regex Patterns)
At the bottom of the window, you can expand the **Regex Patterns** section. Here you can add simple words or complex regex patterns. 
If the plugin reads a matching message in your Party or Cross-World Party chat, it will instantly reset your counter to zero, assuming a new pull or instance is starting!
