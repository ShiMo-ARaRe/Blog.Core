using System.IO;

namespace Blog.Core.Common.AppConfig
{
    /// <summary>
    /// 辅助于JwtHelper类，拿Audience配置信息用
    /// </summary>
    public class AppSecretConfig
    {
        // 从AppSettings中获取"Audience"和"Secret"的值，并存储在静态变量Audience_Secret中
        private static string Audience_Secret = AppSettings.app(new string[] { "Audience", "Secret" });
        // 从AppSettings中获取"Audience"和"SecretFile"的值，并存储在静态变量Audience_Secret_File中
        private static string Audience_Secret_File = AppSettings.app(new string[] { "Audience", "SecretFile" });

        public static string Audience_Secret_String => InitAudience_Secret();

        // 初始化Audience_Secret的方法
        private static string InitAudience_Secret()
        {
            // 调用DifDBConnOfSecurity方法，传入Audience_Secret_File作为参数，获取securityString
            var securityString = DifDBConnOfSecurity(Audience_Secret_File);

            // 如果Audience_Secret_File不为空且securityString不为空
            if (!string.IsNullOrEmpty(Audience_Secret_File) && !string.IsNullOrEmpty(securityString))
            {
                // 返回securityString
                return securityString;
            }
            else
            {
                // 返回Audience_Secret
                return Audience_Secret;
            }
        }

        // 根据给定的文件路径参数，返回文件内容的方法
        private static string DifDBConnOfSecurity(params string[] conn)
        {
            // 遍历conn数组中的每个元素
            foreach (var item in conn)
            {
                try
                {
                    // 如果文件存在
                    if (File.Exists(item))
                    {
                        // 返回文件的文本内容，并去除两端的空格
                        return File.ReadAllText(item).Trim();
                    }
                }
                catch (System.Exception) { }
            }

            // 如果没有找到文件或发生异常，则返回空字符串
            return "";
        }
    }

}
