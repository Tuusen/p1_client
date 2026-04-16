using System.Collections.Generic;

namespace GeometryTD
{
    public static class ViewPathManager
    {
        private static readonly Dictionary<string, string> pathMap = new Dictionary<string, string>
        {
            { "SkillSelectWin", "UI/Windows/SkillSelectWin" },
            { "HeroSelectWin", "UI/Windows/HeroSelectWin" },
        };

        /// <summary>
        /// 注册窗口名到预制体路径的映射。
        /// </summary>
        public static void Register(string winName, string prefabPath)
        {
            pathMap[winName] = prefabPath;
        }

        /// <summary>
        /// 根据窗口名获取预制体路径。
        /// 优先查找已注册的映射，未找到则回退到默认路径 "UI/Windows/{winName}"。
        /// </summary>
        public static string GetPath(string winName)
        {
            if (pathMap.TryGetValue(winName, out string path))
                return path;
            return $"UI/Windows/{winName}";
        }
    }
}
