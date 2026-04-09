using System.Collections.Generic;

namespace HLL_ATassistant
{
    public partial class OverlayForm
    {
        #region 语言字典

        private static readonly Dictionary<string, string> ChineseTexts = new Dictionary<string, string>
        {
            { "DisplayOutofCali", "超出校准范围 (最大 {0:F1})" },
            { "DisplayOutofAngle", "角度超出有效范围" },
            { "DisplayWaitforCali", "等待校准..." },
            { "DisplayNoMovement", "D: ---  θ: 0°" },
        };

        private static readonly Dictionary<string, string> EnglishTexts = new Dictionary<string, string>
        {
            { "DisplayOutofCali", "Out of calibration range (max {0:F1})" },
            { "DisplayOutofAngle", "Angle out of range" },
            { "DisplayWaitforCali", "Waiting for calibration..." },
            { "DisplayNoMovement", "D: ---  θ: 0°" },
        };

        #endregion

        /// <summary>
        /// 获取本地化字符串
        /// </summary>
        private string GetString(string key, params object[] args)
        {
            var texts = _currentLanguage == MainForm.Language.Chinese ? ChineseTexts : EnglishTexts;
            if (texts.TryGetValue(key, out string template))
            {
                return args.Length > 0 ? string.Format(template, args) : template;
            }
            return key;
        }

        /// <summary>
        /// 应用语言（由 MainForm 切换语言时调用）
        /// </summary>
        public void ApplyLanguage()
        {
            // 同步 MainForm 的语言设置
            _currentLanguage = _mainForm.GetCurrentLanguage();
            UpdateDisplay();
        }
    }
}