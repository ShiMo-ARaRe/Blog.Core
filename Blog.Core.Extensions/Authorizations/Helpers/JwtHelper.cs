using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Blog.Core.Common;
using Blog.Core.Common.AppConfig;
using Microsoft.IdentityModel.Tokens;

namespace Blog.Core.AuthHelper.OverWrite
{
    public class JwtHelper
    {

        /// <summary>
        /// 颁发JWT字符串（token
        /// </summary>
        /// <param name="tokenModel"></param>
        /// <returns></returns>
        public static string IssueJwt(TokenModelJwt tokenModel)
        {
            string iss = AppSettings.app(new string[] { "Audience", "Issuer" });    //发行者
            string aud = AppSettings.app(new string[] { "Audience", "Audience" });  //受众
            string secret = AppSecretConfig.Audience_Secret_String; //密钥

            //var claims = new Claim[] //old
            var claims = new List<Claim>    //声明
                {
                 /*
                 * 特别重要：
                   1、这里将用户的部分信息，比如 uid 存到了Claim 中，如果你想知道如何在其他地方将这个 uid从 Token 中取出来，请看下边的SerializeJwt() 方法，或者在整个解决方案，搜索这个方法，看哪里使用了！
                   2、你也可以研究下 HttpContext.User.Claims ，具体的你可以看看 Policys/PermissionHandler.cs 类中是如何使用的。
                 */

                    
                //用于存储 JWT 的唯一标识符（JTI）
                new Claim(JwtRegisteredClaimNames.Jti, tokenModel.Uid.ToString()),
                //用于存储 JWT 的发布时间（IAT）
                new Claim(JwtRegisteredClaimNames.Iat, $"{DateTime.Now.DateToTimeStamp()}"),
                //用于存储 JWT 的生效时间（NBF）
                new Claim(JwtRegisteredClaimNames.Nbf,$"{DateTime.Now.DateToTimeStamp()}") ,
                //用于存储 JWT 的过期时间（EXP），值为当前时间加上1000秒后的时间的 Unix 时间戳字符串表示形式，注意JWT有自己的缓冲过期时间
                new Claim (JwtRegisteredClaimNames.Exp,$"{new DateTimeOffset(DateTime.Now.AddSeconds(1000)).ToUnixTimeSeconds()}"),
                //用于存储 JWT 的过期时间（Expiration），值为当前时间加上1000秒后的时间的字符串表示形式
                new Claim(ClaimTypes.Expiration, DateTime.Now.AddSeconds(1000).ToString()),
                new Claim(JwtRegisteredClaimNames.Iss,iss), //用于存储 JWT 的发行者（ISS）
                new Claim(JwtRegisteredClaimNames.Aud,aud), //用于存储 JWT 的受众（AUD）
                
                //new Claim(ClaimTypes.Role,tokenModel.Role),//为了解决一个用户多个角色(比如：Admin,System)，用下边的方法
               };

            // 可以将一个用户的多个角色全部赋予；
            // 作者：DX 提供技术支持；
            // 将一个字符串（tokenModel.Role）拆分成多个部分，并将每个部分作为角色（ClaimTypes.Role）添加到声明列表（claims）中
            claims.AddRange(tokenModel.Role.Split(',').Select(s => new Claim(ClaimTypes.Role, s)));



            //秘钥 (SymmetricSecurityKey 对安全性的要求，密钥的长度太短会报出异常)
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            // 创建对称加密密钥（SymmetricSecurityKey），使用指定的 secret 字符串经过 UTF-8 编码后得到的字节数组

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            // 创建签名凭证（SigningCredentials），使用上述密钥和指定的签名算法（HMACSHA256）

            var jwt = new JwtSecurityToken(
                issuer: iss,
                claims: claims,
                signingCredentials: creds);
            // 创建 JWT 令牌（JwtSecurityToken），指定发行者（issuer）、声明（claims）和签名凭证（signingCredentials）

            var jwtHandler = new JwtSecurityTokenHandler();// 创建 JWT 令牌处理程序（JwtSecurityTokenHandler）

            var encodedJwt = jwtHandler.WriteToken(jwt);
            // 将生成的 JWT 令牌（jwt）序列化为字符串表示形式（编码为 Base64），得到 encodedJwt

            return encodedJwt;// 返回生成的 JWT 令牌字符串
        }

        /// <summary>
        /// 解析JWT字符串（token
        /// </summary>
        /// <param name="jwtStr"></param>
        /// <returns></returns>
        public static TokenModelJwt SerializeJwt(string jwtStr)
        {
            //创建 JWT 令牌处理程序（JwtSecurityTokenHandler）的实例。
            var jwtHandler = new JwtSecurityTokenHandler();
            //创建一个空的 TokenModelJwt 对象，用于存储 JWT 的信息。
            TokenModelJwt tokenModelJwt = new TokenModelJwt();

            // token校验
            if (jwtStr.IsNotEmptyOrNull() && jwtHandler.CanReadToken(jwtStr))
            //进行 JWT 字符串的校验，确保字符串不为空且可以被正确读取。
            {
                //使用 JWT 令牌处理程序读取 JWT 字符串，将其转换为 JwtSecurityToken 对象。
                JwtSecurityToken jwtToken = jwtHandler.ReadJwtToken(jwtStr);

                object role;
                //从 JWT 的负载（Payload）中尝试获取声明（Claim）中的角色信息，并将其存储在 role 变量中。
                jwtToken.Payload.TryGetValue(ClaimTypes.Role, out role);

                tokenModelJwt = new TokenModelJwt
                {
                /*  根据 JWT 的信息创建一个新的 TokenModelJwt 对象，
                    将 JWT 中的唯一标识符（jwtToken.Id）转换为长整型并存储在 Uid 属性中，将角色信息存储在 Role 属性中。*/
                    Uid = (jwtToken.Id).ObjToLong(),
                    Role = role != null ? role.ObjToString() : "",
                };
            }
            return tokenModelJwt;
        }

        /// <summary>
        /// 进行自定义的 JWT 安全验证
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public static bool customSafeVerify(string token)
        {
            //创建 JWT 令牌处理程序（JwtSecurityTokenHandler）的实例
            var jwtHandler = new JwtSecurityTokenHandler();
            //获取存储在 AppSecretConfig.Audience_Secret_String 中的对称密钥作为 Base64 字符串表示。
            var symmetricKeyAsBase64 = AppSecretConfig.Audience_Secret_String;
            //将对称密钥的 Base64 字符串表示转换为 ASCII 编码的字节数组。
            var keyByteArray = Encoding.ASCII.GetBytes(symmetricKeyAsBase64);
            //用上述字节数组创建对称加密密钥（SymmetricSecurityKey）。
            var signingKey = new SymmetricSecurityKey(keyByteArray);
            //创建签名凭证（SigningCredentials），使用上述密钥和指定的签名算法（HMACSHA256）。
            var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            //使用 JWT 令牌处理程序读取传入的 JWT 字符串，将其转换为 JwtSecurityToken 对象。
            var jwt = jwtHandler.ReadJwtToken(token);
            /*  对 JWT 进行自定义的安全验证。比较 JWT 的原始签名（RawSignature）与根据原始头部和负载计算得到的签名是否相同。
             *  如果相同，则返回 true，表示 JWT 验证通过；否则返回 false，表示验证失败。*/
            return jwt.RawSignature == Microsoft.IdentityModel.JsonWebTokens.JwtTokenUtilities.CreateEncodedSignature(jwt.RawHeader + "." + jwt.RawPayload, signingCredentials);
        }
    }

    /// <summary>
    /// 令牌
    /// </summary>
    public class TokenModelJwt
    {
        /// <summary>
        /// Id
        /// </summary>
        public long Uid { get; set; }
        /// <summary>
        /// 角色
        /// </summary>
        public string Role { get; set; }
        /// <summary>
        /// 职能
        /// </summary>
        public string Work { get; set; }

    }
}
