using System;
using System.Collections.Generic;
using System.Linq;

namespace PingMonitor
{
    public static class ServerCatalog
    {
        public static List<ServerDefinition> Create()
        {
            List<ServerDefinition> servers = new List<ServerDefinition>
            {
                new ServerDefinition("sa", "الرياض", "STC", "speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "جدة", "STC", "jed-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "المدينة المنورة", "STC", "ab-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "تبوك", "STC", "tabuk-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "جازان", "STC", "jizan-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "نجران", "STC", "najran-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "الدمام", "STC", "dam-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "مكة", "STC", "makkah-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "ينبع", "STC", "yanbu-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "الحمراء", "STC", "alhamraa-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "أبحر", "STC", "obhur-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "بحرة", "STC", "bahrah-speedtest.saudi.net.sa"),
                new ServerDefinition("sa", "أبيار علي", "STC", "abyarali-speedtest.saudi.net.sa"),

                new ServerDefinition("sa", "الرياض", "Zain", "speedtest-riyadhnew.sa.zain.com.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "جدة", "Zain", "speedtest-jeddahnew.sa.zain.com.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "المدينة المنورة", "Zain", "speedtest-medina.sa.zain.com.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "تبوك", "Zain", "speedtest-tabuk.sa.zain.com.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "القصيم", "Zain", "speedtest-qassim.sa.zain.com.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "خميس مشيط", "Zain", "speedtest-khamismushait.sa.zain.com.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "الهفوف", "Zain", "speedtest-hofuf.sa.zain.com.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "الدمام", "Zain", "speedtest-dammamnew.sa.zain.com.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "مكة", "Zain", "speedtest-makkah.sa.zain.com.prod.hosts.ooklaserver.net"),

                new ServerDefinition("sa", "الرياض", "Mobily", "ryd.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "جدة", "Mobily", "jed.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "المدينة المنورة", "Mobily", "mdn.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "تبوك", "Mobily", "tbk.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "جازان", "Mobily", "jzn.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "نجران", "Mobily", "njr.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "الدمام", "Mobily", "dam.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "ينبع", "Mobily", "ynb.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "بحرة", "Mobily", "bhr.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "المشاعر", "Mobily", "msr.myspeed.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "مكة", "Mobily", "mkh.myspeed.net.sa.prod.hosts.ooklaserver.net"),

                new ServerDefinition("sa", "الرياض", "Salam", "ftthspeed-ruh.salam.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "جدة", "Salam", "jed-speed.itc.net.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "الخبر", "Salam", "speedtest-kbr.salam.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "الرياض", "GO Telecom", "speedtest.go.com.sa.prod.hosts.ooklaserver.net"),
                new ServerDefinition("sa", "جدة", "GO Telecom", "hispeedtest.go.com.sa.prod.hosts.ooklaserver.net"),

                new ServerDefinition("ae", "أبو ظبي", "Etisalat", "speedtest2.etisalat.ae"),
                new ServerDefinition("ae", "دبي", "Etisalat", "speedtest1.etisalat.ae"),
                new ServerDefinition("ae", "أبو ظبي", "du", "auh.speedtest.du.ae"),
                new ServerDefinition("ae", "دبي", "du", "dxbsouth.speedtest.du.ae.prod.hosts.ooklaserver.net"),

                new ServerDefinition("bh", "الرفاع", "STC Bahrain", "speedtest4.stc.com.bh.prod.hosts.ooklaserver.net"),
                new ServerDefinition("bh", "سيف", "STC Bahrain", "speedtest3.stc.com.bh.prod.hosts.ooklaserver.net"),
                new ServerDefinition("bh", "المنامة", "STC Bahrain", "speedtest2.stc.com.bh.prod.hosts.ooklaserver.net"),
                new ServerDefinition("bh", "المحرق", "STC Bahrain", "speedtest1.stc.com.bh.prod.hosts.ooklaserver.net"),
                new ServerDefinition("bh", "توبلي", "Zain Bahrain", "staging.bh.zain.com"),
                new ServerDefinition("bh", "المنامة", "Zain Bahrain", "stest.bh.zain.com"),
                new ServerDefinition("bh", "المنامة", "Etisalcom", "sp1.etisalcom.com.prod.hosts.ooklaserver.net"),

                new ServerDefinition("i_n", "مومباي", "Amazon", "dynamodb.ap-south-1.amazonaws.com"),
                new ServerDefinition("de", "فرانكفورت", "Amazon", "dynamodb.eu-central-1.amazonaws.com"),
                new ServerDefinition("fr", "باريس", "Amazon", "dynamodb.eu-west-3.amazonaws.com"),
                new ServerDefinition("gb", "لندن", "Amazon", "dynamodb.eu-west-2.amazonaws.com"),
                new ServerDefinition("es", "إسپانيا", "Amazon", "dynamodb.eu-south-2.amazonaws.com"),
                new ServerDefinition("ie", "أيرلندا", "Amazon", "dynamodb.eu-west-1.amazonaws.com"),
                new ServerDefinition("it", "ميلانو", "Amazon", "dynamodb.eu-south-1.amazonaws.com"),
                new ServerDefinition("ch", "زيورخ", "Amazon", "dynamodb.eu-central-2.amazonaws.com"),
                new ServerDefinition("se", "ستوكهولم", "Amazon", "dynamodb.eu-north-1.amazonaws.com"),

                new ServerDefinition("bh", "المنامة", "Amazon", "dynamodb.me-south-1.amazonaws.com")
            };

            if (servers.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
                throw new InvalidOperationException("The server catalog contains duplicate server IDs.");
            return servers;
        }

        public static Dictionary<string, string> GetLegacyIdMap()
        {
            Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string[] codes = { "sa", "bh", "de", "fr", "gb", "es" };
            string[] locations = { "الدمام", "المنامة", "فرانكفورت", "باريس", "لندن", "إسپانيا" };
            for (int i = 0; i < codes.Length; i++)
            {
                string oldId = new ServerDefinition(codes[i], locations[i], "Google Cloud", "").Id;
                string newId = new ServerDefinition(codes[i], i == 0 ? "الرياض" : locations[i], i == 0 ? "Salam" : "Amazon", "").Id;
                aliases.Add(oldId, newId);
            }
            return aliases;
        }
    }
}
