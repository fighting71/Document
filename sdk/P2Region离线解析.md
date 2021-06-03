# P2Region离线解析 #

**NuGet扩展包**：IP2Region

	Report Abuse ： https://www.nuget.org/packages/IP2Region/1.2.0/ReportAbuse

[github](https://github.com/lionsoul2014/ip2region)

#### 使用代码： ####


	using IP2Region;
	using IP2Region.Models;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	
	namespace UA.Sdk.IpSdk
	{
	
	    /// <summary>
	    /// P2Region离线解析
	    /// </summary>
	    public class P2RegionHelper
	    {
	        /// <summary>
	        /// 文件存储位置
	        /// </summary>
	        protected const string FilePath = "config_data/ip2region.db";
	        /// <summary>
	        /// 查找结果缓存 [region]
	        /// </summary>
	        protected static Dictionary<string, string> _cache = new Dictionary<string, string>();
	
	        protected static DbSearcher _dbSearch;
	
	        /// <summary>
	        /// 参考格式 :
	        /// 英国|0|Sheffield|0|0
	        /// 中国|0|浙江省|金华市|移动
	        /// </summary>
	        /// <param name="ip"></param>
	        /// <returns></returns>
	        public static async Task<string> GetRegionAsync(string ip)
	        {
	            if (_cache.TryGetValue(ip, out var region)) return region;
	
	            Init();
	
	            try
	            {
	                DataBlock dataBlock = await _dbSearch.BinarySearchAsync(ip);
	                return _cache[ip] = dataBlock.Region;
	            }
	            catch (Exception)// 异常直接返回空.
	            {
	                return _cache[ip] = string.Empty;
	            }
	        }
	
	        public static async Task<string> GetPostionAsync(string ip)
	        {
	            string region = await GetRegionAsync(ip);
	
	            if (!string.IsNullOrEmpty(region))
	            {
	                return AnalySis(region);
	            }
	            return null;
	        }
	
	        /// <summary>
	        /// 解析region信息
	        /// </summary>
	        /// <param name="region"></param>
	        private static string AnalySis(string region)
	        {
	            int lineCount = 0;
	            StringBuilder builder = new StringBuilder();
	            bool beforeIsChar = false;
	            for (int i = 0; i < region.Length; i++)
	            {
	                if (region[i] == '|')
	                {
	                    if (lineCount == 3) break;
	                    if (beforeIsChar)
	                        builder.Append(' ');
	                    lineCount++;
	                    beforeIsChar = false;
	                }
	                else if (region[i] == '0') continue;
	                else
	                {
	                    builder.Append(region[i]);
	                    beforeIsChar = true;
	                }
	            }
	
	            return builder.ToString();
	        }
	
	        /// <summary>
	        /// 初始化
	        /// </summary>
	        private static void Init()
	        {
	            _dbSearch ??= new DbSearcher(FilePath);
	        }
	    }
	}

#### Ip类型： ####


    // A类ip : 1~126.0~255.0~255.1~254
    var ip = $"{rand.Next(126) + 1}.{rand.Next(255)}.{rand.Next(255)}.{rand.Next(254) + 1}";


----------
6/3/2021 9:41:02 AM 