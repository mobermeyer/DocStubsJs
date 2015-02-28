using System;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace JScriptStubOptions
{
    public class ReturnOptions
    {
        #region OptionProperties

        private bool vsdocEnabled = false;
        public bool VSDocEnabled
        {
            get
            {
                LoadSettingsFromStorage();
                return vsdocEnabled;
            }
            set { vsdocEnabled = value; }
        }

        private bool jsdocEnabled = false;
        public bool JSDocEnabled
        {
            get
            {
                LoadSettingsFromStorage();
                return jsdocEnabled;
            }
            set { jsdocEnabled = value; }
        }

        private bool autoNewLine = false;
        /// <summary>
        /// Whether or not to auto create new comment lines.
        /// </summary>
        public bool AutoNewLine
        {
            get
            {
                LoadSettingsFromStorage();
                return autoNewLine;
            }
            set { autoNewLine = value; }
        }

        private static string returnAttrs = "type=\"\"";
        /// <summary>
        /// The default return tag attributes to use for vsdoc comments.
        /// </summary>
        public string ReturnAttributes
        {
            get
            {
                LoadSettingsFromStorage();
                return returnAttrs;
            }
            internal set { returnAttrs = value; }
        }

        private bool multiLineReturn = false;
        /// <summary>
        /// Whether or not Return tags should be rendered on multiple lines.
        /// </summary>
        public bool MultiLineReturn
        {
            get 
            {
                LoadSettingsFromStorage();
                return multiLineReturn; 
            }
            set { multiLineReturn = value; }
        }

        private bool useAsterisk = true;
        /// <summary>
        /// Whether or not new comment lines should begin with an asterisk (*).
        /// </summary>
        public bool UseAsterisk
        {
            get
            {
                LoadSettingsFromStorage();
                return useAsterisk;
            }
            set { useAsterisk = value; }
        }

        private ReturnTagGenerationSetting returnGenerationOption = ReturnTagGenerationSetting.Auto;
        /// <summary>
        /// Gets or sets the value indicating when return tags should be generated.
        /// </summary>
        public ReturnTagGenerationSetting ReturnGenerationOption
        {
            get
            {
                LoadSettingsFromStorage();
                return returnGenerationOption;
            }
            set { returnGenerationOption = value; }
        }

        private static string paramAttrs = "";
        /// <summary>
        /// The default param tag attributes to use for vsdoc comments.
        /// </summary>
        public string ParamAttributes
        {
            get
            {
                LoadSettingsFromStorage();
                return paramAttrs;
            }
            internal set { paramAttrs = value; }
        }

        private bool multiLineParam = false;
        /// <summary>
        /// Whether or not Param tags should be rendered on multiple lines.
        /// </summary>
        public bool MultiLineParam
        {
            get 
            {
                LoadSettingsFromStorage();
                return multiLineParam;
            }
            set { multiLineParam = value; }
        }

        private bool multiLineSummary = true;
        /// <summary>
        /// Whether or not Summary tags should be rendered on multiple lines.
        /// </summary>
        public bool MultiLineSummary
        {
            get 
            {
                LoadSettingsFromStorage();
                return multiLineSummary; 
            }
            set { multiLineSummary = value; }
        }

        private bool useSpacesForTabs = true;
        /// <summary>
        /// Whether or not Spaces should be inserted in place of tabs.
        /// </summary>
        public bool UseSpacesForTabs
        {
            get
            {
                LoadSettingsFromStorage();
                return useSpacesForTabs;
            }
            set { useSpacesForTabs = value; }
        }

        private int spacesForTabsCount = 4;
        /// <summary>
        /// The number of spaces to insert when UseSpacesForTabs is true.
        /// </summary>
        public int SpacesForTabsCount
        {
            get
            {
                LoadSettingsFromStorage();
                return spacesForTabsCount;
            }
            set { spacesForTabsCount = value; }
        }
        #endregion

        #region Properties
        private static bool optionsChanged = false;

        public static bool OptionsChanged
        {
            get { return optionsChanged; }
            set { optionsChanged = value; }
        }
        #endregion

        public ReturnOptions()
        {
            OptionsChanged = true;
            LoadSettingsFromStorage();
        }

        private void LoadSettingsFromStorage()
        {
            if (OptionsChanged)
            {
                // TODO: someday just make UpdateSetting/GetSetting generic
                VSDocEnabled = UpdateSetting("VSDocEnabled", true);
                JSDocEnabled = UpdateSetting("JSDocEnabled", true);
                ReturnAttributes = UpdateSetting("ReturnTagAttributes", "type=\"\"");
                ParamAttributes = UpdateSetting("ParamTagAttributes", "");
                MultiLineParam = UpdateSetting("MultiLineParam", false);
                MultiLineReturn = UpdateSetting("MultiLineReturn", false);
                UseAsterisk = UpdateSetting("UseAsterisk", true);
                MultiLineSummary = UpdateSetting("MultiLineSummary", true);
                AutoNewLine = UpdateSetting("AutoNewLine", true);
                ReturnGenerationOption = UpdateEnumSetting("ReturnGenerationSetting", ReturnTagGenerationSetting.Auto);
                UseSpacesForTabs = GetSetting("Text Editor\\JavaScript", "Insert Tabs", 0) == 0;
                SpacesForTabsCount = GetSetting("Text Editor\\JavaScript", "Tab Size", 4);

                OptionsChanged = false;
            }
        }

        private const string PROP_LOCATION = "DialogPage\\JScriptStubOptions.JScriptStubOptions";
        private bool UpdateSetting(string name, bool defaultValue)
        {
            using (var regKey = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_UserSettings))
            {
                if (regKey == null) return defaultValue;

                using (var jScriptKey = regKey.OpenSubKey("DialogPage\\JScriptStubOptions.JScriptStubOptions"))
                {
                    if (jScriptKey == null) return defaultValue;

                    var prop = jScriptKey.GetValue(name) as string;

                    if (prop == null) return defaultValue;

                    bool result;
                    if (!Boolean.TryParse(prop, out result))
                    {
                        return defaultValue;
                    }
                    return result;
                }
            }
        }

        private T UpdateEnumSetting<T>(string name, T defaultValue)
        {
            using (var regKey = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_UserSettings))
            {
                if (regKey == null) return defaultValue;

                using (var jScriptKey = regKey.OpenSubKey("DialogPage\\JScriptStubOptions.JScriptStubOptions"))
                {
                    if (jScriptKey == null) return defaultValue;

                    var prop = jScriptKey.GetValue(name);

                    if (prop == null) return defaultValue;

                    try
                    {
                        return (T)Enum.Parse(typeof(T), prop.ToString());
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }
        }

        // Updates a setting specific to the JScript vsdoc Stub Generator options
        private string UpdateSetting(string name, string defaultValue)
        {
            return GetSetting("DialogPage\\JScriptStubOptions.JScriptStubOptions", name, defaultValue);
        }

        // Gets any setting from the VisualStudio registry.
        private static T GetSetting<T>(string path, string name, T defaultValue)
        {
            using (var regKey = VSRegistry.RegistryRoot(__VsLocalRegistryType.RegType_UserSettings))
            {
                if (regKey == null) return defaultValue;

                using (var jScriptKey = regKey.OpenSubKey(path))
                {
                    if (jScriptKey == null) return defaultValue;

                    var prop = jScriptKey.GetValue(name);

                    if (prop == null) return defaultValue;

                    return (T)prop;
                }
            }
        }
    }
}
