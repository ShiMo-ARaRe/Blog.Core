using Blog.Core.Model.ViewModels;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Blog.Core.AuthHelper
{
    /// <summary>
    /// JWTToken生成类
    /// </summary>
    public class JwtToken
    {
        /// <summary>
        /// 获取基于JWT的Token
        /// </summary>
        /// <param name="claims">需要在登陆的时候配置</param>
        /// <param name="permissionRequirement">在startup中定义的参数</param>
        /// <returns></returns>
        public static TokenInfoViewModel BuildJwtToken(Claim[] claims, PermissionRequirement permissionRequirement)
        {
            var now = DateTime.Now; //获取当前时间
            // 实例化JwtSecurityToken
            var jwt = new JwtSecurityToken(
                issuer: permissionRequirement.Issuer,   //发行者
                audience: permissionRequirement.Audience,   //受众
                claims: claims, //声明
                notBefore: now, //令牌生效时间
                expires: now.Add(permissionRequirement.Expiration), //过期时间
                signingCredentials: permissionRequirement.SigningCredentials    // 签名凭据等参数
            );
            // 生成 Token
            var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

            //打包返回前台
            var responseJson = new TokenInfoViewModel
            {
                success = true,
                token = encodedJwt,
                expires_in = permissionRequirement.Expiration.TotalSeconds, //过期时间
                token_type = "Bearer"   //令牌类型
            };
            return responseJson;
        }
    }
}
